#include <QJsonDocument>
#include <QJsonObject>
#include "manual_drive_controller.h"
#include <QDoubleSpinBox>
#include <QApplication>
#include <QCursor>
#include "gui_manager.h"
#include "gui_sections/lidar_section.h"

#include <QFrame>
#include <QLabel>
#include <QPushButton>
#include <QStackedWidget>
#include <QScrollArea>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QGridLayout>
#include <QButtonGroup>
#include <QSlider>
#include <QTimer>
#include <QElapsedTimer>
#include <memory>

namespace LunarRoverUI {

namespace {

QLabel *label(const QString &text,
              const QString &name = "")
{
    auto *l = new QLabel(text);
    l->setObjectName(name);
    return l;
}

QVBoxLayout *card(QVBoxLayout *parent,
                  const QString &title)
{
    auto *frame = new QFrame;
    frame->setObjectName("card");

    auto *layout = new QVBoxLayout(frame);
    layout->setContentsMargins(16, 14, 16, 14);
    layout->setSpacing(8);
    layout->addWidget(label(title, "sectionTitle"));

    parent->addWidget(frame);
    return layout;
}

QLabel *metric(QHBoxLayout *parent,
               const QString &title,
               const QString &unit)
{
    auto *frame = new QFrame;
    frame->setObjectName("card");

    auto *layout = new QVBoxLayout(frame);
    layout->setContentsMargins(16, 12, 16, 12);

    layout->addWidget(label(title, "metricTitle"));

    auto *value = label("--", "metricValue");
    layout->addWidget(value);
    layout->addWidget(label(unit, "mutedLabel"));

    parent->addWidget(frame, 1);
    return value;
}

struct TelemetryTimes {
    QElapsedTimer battery;
    QElapsedTimer odom;
    QElapsedTimer imu;
    QElapsedTimer scan;
};

bool fresh(const QElapsedTimer &t)
{
    return t.isValid() && t.elapsed() < 3000;
}

QString degrees(double radians)
{
    return QString("%1 deg").arg(
        radians * 57.295779513, 0, 'f', 1);
}

}

GUIManager::GUIManager(
    SimulationBridge *bridge, QObject *parent)
    : QObject(parent), m_simulationBridge(bridge)
{
}

QWidget *GUIManager::createUI(QWidget *parent)
{
    auto *central = new QWidget(parent);
    auto *main = new QVBoxLayout(central);

    main->setContentsMargins(20, 16, 20, 12);
    main->setSpacing(12);

    auto *times = new TelemetryTimes;
    auto clock = std::shared_ptr<TelemetryTimes>(times);

    // Shared mission-control header
    auto *header = new QHBoxLayout;
    auto *heading = new QVBoxLayout;

    heading->addWidget(label("LUNAR ROVER", "pageTitle"));
    heading->addWidget(label(
        "ROS 2 Mission Control  |  Rover 01",
        "subtitle"));

    header->addLayout(heading);
    header->addStretch();

    auto *connection = label(
        "WAITING FOR TELEMETRY", "warningLabel");

    header->addWidget(connection);
    main->addLayout(header);

    // Shared navigation
    auto *nav = new QHBoxLayout;
    auto *dashboardButton = new QPushButton("Dashboard");
    auto *manualButton = new QPushButton("Manual Override");

    dashboardButton->setCheckable(true);
    manualButton->setCheckable(true);

    auto *group = new QButtonGroup(central);
    group->addButton(dashboardButton);
    group->addButton(manualButton);
    dashboardButton->setChecked(true);

    nav->addWidget(dashboardButton);
    nav->addWidget(manualButton);
    nav->addStretch();

    auto *stop = new QPushButton("STOP - Disabled");
    stop->setObjectName("stopButton");
    stop->setEnabled(false);
    stop->setToolTip("Motion control is not authorized.");

    nav->addWidget(stop);
    main->addLayout(nav);

    auto *pages = new QStackedWidget;
    main->addWidget(pages, 1);

    // Dashboard
    auto *scroll = new QScrollArea;
    scroll->setWidgetResizable(true);

    auto *dashboard = new QWidget;
    auto *dash = new QVBoxLayout(dashboard);
    dash->setContentsMargins(0, 0, 4, 0);
    dash->setSpacing(12);

    auto *metrics = new QHBoxLayout;
    metrics->setSpacing(12);

    auto *battery = metric(metrics,
        "BATTERY VOLTAGE", "Volts");

    auto *linear = metric(metrics,
        "LINEAR VELOCITY", "m/s - measured");

    auto *angular = metric(metrics,
        "ANGULAR VELOCITY", "rad/s - measured");

    dash->addLayout(metrics);

    auto *lidar = new LidarSection(dashboard);
    dash->addWidget(lidar, 1);

    auto *bottom = new QHBoxLayout;
    bottom->setSpacing(12);

    auto *left = new QVBoxLayout;
    auto *right = new QVBoxLayout;

    auto *location = card(left, "LOCALIZATION");

    auto *x = label("X position: -- m");
    auto *y = label("Y position: -- m");
    auto *yaw = label("Heading: --");
    auto *roll = label("Roll: --");
    auto *pitch = label("Pitch: --");

    location->addWidget(x);
    location->addWidget(y);
    location->addWidget(yaw);
    location->addWidget(roll);
    location->addWidget(pitch);

    auto *mission = card(right, "MISSION OBJECTIVES");

    mission->addWidget(label("PLANNED GOAL", "mutedLabel"));
    mission->addWidget(label("Fetch Water", "sectionTitle"));
    mission->addWidget(label("Status: Awaiting Unity"));
    mission->addWidget(label("ETA: Not available"));
    mission->addStretch();

    bottom->addLayout(left, 1);
    bottom->addLayout(right, 1);
    dash->addLayout(bottom);

    scroll->setWidget(dashboard);
    pages->addWidget(scroll);

    // Manual override - isolated ROS testing
    using Controller = ManualDriveController;
    using Direction = Controller::Direction;
    using State = Controller::State;

    auto controller = std::make_shared<Controller>();

    // GUI-side authorization state. Accessed only on the Qt thread.
    struct ArbiterFeedback {
        QString state = "unknown";
        QString reason = "Awaiting arbiter";
        bool odometryFresh = false;
        bool valid = false;

        QElapsedTimer lastFeedback;
        QElapsedTimer requestTimer;

        quint64 requestId = 0;
        bool authorizationPending = false;
        bool authorizationAccepted = false;
        bool resetPending = false;
        bool resetAccepted = false;
        bool stopConfirmed = false;

        bool stopPending = false;
        bool localStopLatched = false;

        bool isFresh() const {
            return valid
                && lastFeedback.isValid()
                && lastFeedback.elapsed() <= 500;
        }

        bool manualAuthorized() const {
            return isFresh()
                && odometryFresh
                && state == "manual";
        }
    };

    auto arbiter = std::make_shared<ArbiterFeedback>();

    auto bridge = m_simulationBridge;

    auto *manual = new QWidget;
    auto *ml = new QVBoxLayout(manual);

    ml->addWidget(label("MANUAL OVERRIDE"));

    // Physical manual mode requires explicit startup configuration.
    const bool physicalMode =
        qgetenv("ROVER_EXECUTION_MODE") == "physical_manual"
        && qgetenv("ROS_DOMAIN_ID") == "20"
        && qgetenv("ROS_LOCALHOST_ONLY") == "0";

    const bool simulationMode =
        qgetenv("ROVER_EXECUTION_MODE").isEmpty()
        && qgetenv("ROS_DOMAIN_ID") == "77"
        && qgetenv("ROS_LOCALHOST_ONLY") == "1";

    const bool validMode = physicalMode || simulationMode;

    auto *warning = label(
        physicalMode
            ? "PHYSICAL MANUAL | Elevated-wheel testing only"
            : simulationMode
                ? "TEST MODE | Physical movement locked"
                : "INVALID EXECUTION CONFIGURATION | CONTROL LOCKED");
    warning->setWordWrap(true);
    ml->addWidget(warning);

    auto *speed = card(ml, "TARGET VELOCITY");

    auto addSpeedControl = [&](const QString &name,
                               int maximum, int initial) {
        speed->addWidget(label(name));

        auto *row = new QHBoxLayout;
        auto *slider = new QSlider(Qt::Horizontal);
        auto *input = new QDoubleSpinBox;

        slider->setRange(0, maximum);
        slider->setValue(initial);

        input->setDecimals(3);
        input->setRange(0, maximum / 1000.0);
        input->setSingleStep(0.005);
        input->setValue(initial / 1000.0);

        row->addWidget(slider, 1);
        row->addWidget(input);
        speed->addLayout(row);

        connect(slider, &QSlider::valueChanged, input,
            [=](int value) {
                input->setValue(value / 1000.0);
            });

        connect(input,
            QOverload<double>::of(&QDoubleSpinBox::valueChanged),
            slider, [=](double value) {
                slider->setValue(qRound(value * 1000));
            });

        return input;
    };

    auto *linearInput = addSpeedControl(
        "Linear velocity (m/s)", physicalMode ? 50 : 100, 50);

    auto *angularInput = addSpeedControl(
        "Angular velocity (rad/s)", physicalMode ? 150 : 300, 150);

    auto *controls = card(ml, "DIRECTIONAL CONTROL");

    auto *dpad = new QGridLayout;
    controls->addLayout(dpad);

    auto *forward = new QPushButton("Forward");
    auto *reverse = new QPushButton("Reverse");
    auto *rotateLeftButton = new QPushButton("Rotate Left");
    auto *rotateRightButton = new QPushButton("Rotate Right");
    auto *centerStop = new QPushButton("STOP");

    centerStop->setObjectName("stopButton");

    dpad->addWidget(forward, 0, 1);
    dpad->addWidget(rotateLeftButton, 1, 0);
    dpad->addWidget(centerStop, 1, 1);
    dpad->addWidget(rotateRightButton, 1, 2);
    dpad->addWidget(reverse, 2, 1);

    auto *feedback = card(ml, "VELOCITY FEEDBACK");

    auto *commanded = label("Requested velocity: Not published");
    auto *manualVelocity = label("Measured velocity: --");

    feedback->addWidget(commanded);
    feedback->addWidget(manualVelocity);

    auto *safety = card(ml, "CONTROL INTERLOCKS");

    auto *stateLabel = label("Control state: DISARMED");
    auto *odomStatus = label("Odometry: Unavailable");

    safety->addWidget(stateLabel);
    safety->addWidget(odomStatus);
    safety->addWidget(label("Hardware motor watchdog: NOT VERIFIED"));
    safety->addWidget(label(physicalMode
        ? "Autonomous control: DISABLED"
        : "Autonomous control: Arbiter managed"));
    safety->addWidget(label(physicalMode
        ? "Physical mode requested - arbiter confirmation required"
        : "Physical authorization: LOCKED"));

    auto *arm = new QPushButton(physicalMode
        ? "ARM PHYSICAL CONTROL" : "ARM TEST CONTROL");
    safety->addWidget(arm);

    auto *resetStop = new QPushButton("RESET STOP");
    resetStop->setEnabled(false);
    safety->addWidget(resetStop);


    ml->addStretch();

    auto *manualScroll = new QScrollArea;
    manualScroll->setWidgetResizable(true);
    manualScroll->setWidget(manual);
    pages->addWidget(manualScroll);

    auto *driveTimer = new QTimer(manual);
    driveTimer->setTimerType(Qt::PreciseTimer);
    driveTimer->setInterval(100);


    // Validate incoming feedback before using it for authorization.
    connect(bridge, &SimulationBridge::controlStateReceived,
            manual, [=](const QString &json) {

        QJsonParseError error;
        auto document = QJsonDocument::fromJson(
            json.toUtf8(), &error);

        arbiter->valid = false;

        if (error.error != QJsonParseError::NoError
            || !document.isObject())
            return;

        const auto object = document.object();
        const auto state = object.value("state").toString();

        if (state != "idle"
            && state != "manual"
            && state != "auto"
            && state != "stop_latched")
            return;

        if (!object.value("odometry_fresh").isBool()
            || !object.value("physical_execution_enabled").isBool()
            || !validMode
            || object.value("physical_execution_enabled").toBool()
                != physicalMode)
            return;

        arbiter->state = state;
        arbiter->reason = object.value("reason").toString();
        arbiter->odometryFresh =
            object.value("odometry_fresh").toBool();

        arbiter->valid = true;
        arbiter->lastFeedback.start();

    }, Qt::QueuedConnection);


    // Local operator interlocks, independent of ROS authorization.
    auto operatorReady = [=]() {
        return validMode
            && bridge->isConnected()
            && manual->isVisible()
            && manual->window()->isActiveWindow()
            && QApplication::applicationState()
                == Qt::ApplicationActive;
    };

    auto ready = [=]() {
        return operatorReady()
            && arbiter->manualAuthorized()
            && arbiter->authorizationAccepted
            && !arbiter->localStopLatched
            && !arbiter->stopPending
            && !arbiter->resetPending;
    };

    auto refresh = [=]() {
        const bool armed =
            controller->state() == State::Armed && ready();

        forward->setEnabled(armed);
        reverse->setEnabled(armed);
        rotateLeftButton->setEnabled(armed);
        rotateRightButton->setEnabled(armed);

        const bool stopped = arbiter->localStopLatched
            || arbiter->state == "stop_latched";

        const bool canRequest = operatorReady()
            && (!physicalMode || arbiter->state == "idle")
            && arbiter->isFresh()
            && arbiter->odometryFresh
            && !stopped
            && !arbiter->stopPending
            && !arbiter->resetPending;

        arm->setEnabled(armed
            || arbiter->authorizationPending
            || canRequest);


        resetStop->setEnabled(
            arbiter->isFresh()
            && arbiter->state == "stop_latched"
            && !arbiter->stopPending
            && !arbiter->resetPending);

        arm->setText(
            arbiter->authorizationPending ? "CANCEL ARM REQUEST" :
            armed
                ? (physicalMode ? "DISARM PHYSICAL CONTROL"
                                : "DISARM TEST CONTROL")
                : (physicalMode ? "ARM PHYSICAL CONTROL"
                                : "ARM TEST CONTROL"));

        if (arbiter->stopPending)
            stateLabel->setText("Control state: STOP REQUESTED");

        else if (arbiter->resetPending)
            stateLabel->setText("Control state: RESET PENDING");
        else if (arbiter->localStopLatched
                 && (!arbiter->stopConfirmed
                     || !arbiter->isFresh()
                     || arbiter->state != "stop_latched"))
            stateLabel->setText(
                "STOP UNCONFIRMED - LOCAL LOCKOUT");
        else if (stopped)
            stateLabel->setText("Control state: STOP LOCKOUT");
        else if (!arbiter->isFresh())
            stateLabel->setText("Control state: ARBITER UNAVAILABLE");
        else if (arbiter->authorizationPending)
            stateLabel->setText("Control state: AUTHORIZATION PENDING");
        else if (armed)
            stateLabel->setText(physicalMode
                ? "Control state: MANUAL - PHYSICAL"
                : "Control state: MANUAL - TEST ONLY");
        else
            stateLabel->setText(
                "Arbiter state: " + arbiter->state.toUpper());

        odomStatus->setText(
            arbiter->isFresh() && arbiter->odometryFresh
                ? "Odometry: Fresh (arbiter verified)"
                : "Odometry: Unavailable or stale");
    };

    auto publish = [=]() {
        auto v = controller->command(ready());
        bridge->sendVelocityCommand(v.linear, v.angular);
    };

    auto disarm = [=]() {
        const bool hadManualSession =
            controller->state() == State::Armed
            || arbiter->authorizationPending
            || arbiter->authorizationAccepted;

        // Invalidate outstanding authorization responses.
        arbiter->authorizationPending = false;
        arbiter->authorizationAccepted = false;

        controller->disarm();
        driveTimer->stop();

        if (hadManualSession) {
            bridge->sendVelocityCommand(0.0, 0.0);
            bridge->requestDisarm(++arbiter->requestId);
        }

        refresh();
    };


    // Immediately lock local controls and request centralized STOP.
    auto stopControl = [=]() {
        arbiter->authorizationPending = false;
        arbiter->authorizationAccepted = false;
        arbiter->resetPending = false;
        arbiter->localStopLatched = true;
        arbiter->stopPending = true;
        arbiter->stopConfirmed = false;
        arbiter->resetAccepted = false;
        arbiter->requestTimer.start();

        controller->stop();
        driveTimer->stop();

        bridge->sendVelocityCommand(0.0, 0.0);

        const auto id = ++arbiter->requestId;
        refresh();
        bridge->requestStop(id);
    };

    connect(resetStop, &QPushButton::clicked, manual, [=]() {
        if (!arbiter->isFresh()
            || arbiter->state != "stop_latched"
            || arbiter->stopPending
            || arbiter->resetPending)
            return;

        arbiter->resetPending = true;
        arbiter->resetAccepted = false;
        arbiter->requestTimer.start();

        const auto id = ++arbiter->requestId;
        refresh();
        bridge->requestResetStop(id);
    });

    auto bindDirection = [&](QPushButton *button,
                             Direction direction) {
        button->setMinimumHeight(54);
        button->setAutoRepeat(false);

        connect(button, &QPushButton::pressed, manual, [=]() {
            if (!ready()) {
                disarm();
                return;
            }


            controller->setSpeed(
                linearInput->value(),
                angularInput->value());

            controller->press(direction);
            publish();
        });

        connect(button, &QPushButton::released, manual, [=]() {
            controller->release();

            if (controller->state() == State::Armed)
                publish();
        });
    };

    bindDirection(forward, Direction::Forward);
    bindDirection(reverse, Direction::Reverse);
    bindDirection(rotateLeftButton, Direction::RotateLeft);
    bindDirection(rotateRightButton, Direction::RotateRight);


    // ARM requests permission; it does not grant local authorization.
    connect(arm, &QPushButton::clicked, manual, [=]() {
        if (controller->state() == State::Armed
            || arbiter->authorizationPending
            || arbiter->authorizationAccepted) {
            disarm();
            return;
        }

        if (!operatorReady()
            || (physicalMode && arbiter->state != "idle")
            || !arbiter->isFresh()
            || !arbiter->odometryFresh
            || arbiter->localStopLatched
            || arbiter->state == "stop_latched"
            || arbiter->stopPending
            || arbiter->resetPending)
            return;

        arbiter->authorizationPending = true;
        arbiter->authorizationAccepted = false;
        arbiter->requestTimer.start();

        const auto id = ++arbiter->requestId;

        refresh();
        bridge->requestManualAuthorization(id);
    });

    // Process asynchronous authorization responses on the GUI thread.
    connect(bridge, &SimulationBridge::controlServiceResult,
            manual, [=](quint64 id, QString operation,
                        bool success, QString message) {


        if (operation == "stop") {
            if (id != arbiter->requestId)
                return;

            arbiter->stopPending = false;
            arbiter->stopConfirmed = success;
            refresh();
            return;
        }

        if (operation == "reset_stop") {
            if (id != arbiter->requestId
                || !arbiter->resetPending)
                return;

            arbiter->resetAccepted = success;

            if (!success)
                arbiter->resetPending = false;

            refresh();
            return;
        }

        if (operation != "authorize_manual")
            return;

        // A cancelled request must never reactivate manual control.
        if (id != arbiter->requestId
            || !arbiter->authorizationPending) {
            // Ignore outdated responses without disturbing newer
            // STOP, RESET or authorization requests.
            return;
        }

        if (!success
            || !operatorReady()
            || arbiter->localStopLatched
            || arbiter->requestTimer.elapsed() > 2000) {

            disarm();
            stateLabel->setText(
                "Authorization rejected or expired: " + message);
            return;
        }

        // A successful response alone is insufficient.
        // The safety timer must also confirm fresh manual state.
        arbiter->authorizationAccepted = true;
        refresh();

    }, Qt::QueuedConnection);

    connect(centerStop, &QPushButton::pressed,
            manual, stopControl);

    stop->setText(physicalMode ? "STOP ROVER" : "STOP TEST");
    stop->setEnabled(true);
    stop->setToolTip(physicalMode
        ? "Request software STOP and latch manual control"
        : "Stop and latch test commands only");

    connect(stop, &QPushButton::pressed,
            manual, stopControl);


    // Monitor authorization independently of the command timer.
    auto *safetyTimer = new QTimer(manual);
    safetyTimer->setTimerType(Qt::PreciseTimer);
    safetyTimer->setInterval(50);

    connect(safetyTimer, &QTimer::timeout, manual, [=]() {


        // A timed-out service must not clear the local STOP lockout.
        if (arbiter->stopPending
            && arbiter->requestTimer.elapsed() > 2000) {

            arbiter->stopPending = false;
            arbiter->stopConfirmed = false;
            ++arbiter->requestId;
        }

        if (arbiter->resetPending) {
            if (arbiter->requestTimer.elapsed() > 2000) {
                arbiter->resetPending = false;
                arbiter->resetAccepted = false;
                ++arbiter->requestId;
            }
            else if (arbiter->resetAccepted
                     && arbiter->isFresh()
                     && arbiter->state == "idle") {

                controller->resetStop();

                arbiter->localStopLatched = false;
                arbiter->resetPending = false;
                arbiter->resetAccepted = false;
                arbiter->stopConfirmed = false;
            }
        }

        if (arbiter->authorizationPending) {

            const bool expired =
                arbiter->requestTimer.elapsed() > 2000;

            if (expired
                || !operatorReady()
                || !arbiter->isFresh()
                || !arbiter->odometryFresh
                || arbiter->localStopLatched
                || arbiter->stopPending
                || arbiter->state == "stop_latched") {

                disarm();
                return;
            }

            // Require both service acceptance and current authorization.
            if (arbiter->authorizationAccepted
                && arbiter->manualAuthorized()) {

                arbiter->authorizationPending = false;

                if (controller->arm(ready())) {
                    controller->setSpeed(
                        linearInput->value(),
                        angularInput->value());

                    publish();
                    driveTimer->start();
                }
                else {
                    disarm();
                    return;
                }
            }
        }

        if (controller->state() == State::Armed && !ready()) {
            disarm();
            return;
        }

        refresh();
    });

    safetyTimer->start();

    connect(driveTimer, &QTimer::timeout, manual, [=]() {
        if (!ready()) {
            disarm();
            return;
        }

        QPushButton *held = nullptr;

        switch (controller->direction()) {
            case Direction::Forward: held = forward; break;
            case Direction::Reverse: held = reverse; break;
            case Direction::RotateLeft: held = rotateLeftButton; break;
            case Direction::RotateRight: held = rotateRightButton; break;
            default: break;
        }

        if (held && (!held->isDown() ||
            !held->rect().contains(
                held->mapFromGlobal(QCursor::pos())))) {
            disarm();
            return;
        }

        controller->setSpeed(linearInput->value(),
                             angularInput->value());

        odomStatus->setText(fresh(clock->odom)
            ? "Odometry: Receiving"
            : "Odometry: Unavailable or stale");

        publish();
    });

    connect(qApp, &QApplication::applicationStateChanged,
            manual, [=](Qt::ApplicationState state) {
        if (state != Qt::ApplicationActive)
            disarm();
    });

    connect(qApp, &QApplication::aboutToQuit,
            manual, disarm);

    connect(pages, &QStackedWidget::currentChanged,
            manual, [=](int index) {
        if (index != 1)
            disarm();
    });

    connect(dashboardButton, &QPushButton::clicked,
            pages, [=]() { pages->setCurrentIndex(0); });

    connect(manualButton, &QPushButton::clicked,
            pages, [=]() { pages->setCurrentIndex(1); });

    connect(bridge, &SimulationBridge::commandedVelocityUpdated,
            manual, [=](double lv, double av) {
        commanded->setText(
            QString("Requested: %1 m/s | %2 rad/s")
                .arg(lv, 0, 'f', 3)
                .arg(av, 0, 'f', 3));
    });

    refresh();

    // Live telemetry
    connect(m_simulationBridge,
            &SimulationBridge::batteryRawReceived,
            battery, [battery, clock](double raw) {
        clock->battery.restart();
        battery->setText(
            QString::number(raw / 10.0, 'f', 1));
    }, Qt::QueuedConnection);

    connect(m_simulationBridge,
            &SimulationBridge::odometryReceived,
            dashboard,
            [=](double px, double py, double,
                double lv, double av) {
        clock->odom.restart();

        x->setText(QString("X position: %1 m")
            .arg(px, 0, 'f', 3));

        y->setText(QString("Y position: %1 m")
            .arg(py, 0, 'f', 3));

        linear->setText(QString::number(lv, 'f', 3));
        angular->setText(QString::number(av, 'f', 3));

        manualVelocity->setText(
            QString("Measured: %1 m/s  |  %2 rad/s")
                .arg(lv, 0, 'f', 3)
                .arg(av, 0, 'f', 3));

    }, Qt::QueuedConnection);

    connect(m_simulationBridge,
            &SimulationBridge::headingReceived,
            yaw, [yaw](double v) {
        yaw->setText("Heading: " + degrees(v));
    }, Qt::QueuedConnection);

    connect(m_simulationBridge,
            &SimulationBridge::imuOrientationReceived,
            dashboard, [=](double r, double p) {
        clock->imu.restart();
        roll->setText("Roll: " + degrees(r));
        pitch->setText("Pitch: " + degrees(p));
    }, Qt::QueuedConnection);

    connect(m_simulationBridge,
            &SimulationBridge::laserScanReceived,
            lidar, &LidarSection::updateScan,
            Qt::QueuedConnection);

    connect(m_simulationBridge,
            &SimulationBridge::laserScanReceived,
            central,
            [clock](QVector<float>, float, float,
                    float, float) {
        clock->scan.restart();
    }, Qt::QueuedConnection);

    // Telemetry freshness monitoring
    auto *timer = new QTimer(central);

    connect(timer, &QTimer::timeout, central, [=]() {
        bool live = fresh(clock->odom) ||
                    fresh(clock->battery) ||
                    fresh(clock->scan) ||
                    fresh(clock->imu);

        connection->setText(live ?
            "TELEMETRY ONLINE" :
            "NO LIVE TELEMETRY");

        connection->setStyleSheet(live ?
            "color: #34d399; font-weight: bold;" :
            "color: #fbbf24; font-weight: bold;");

        if (!fresh(clock->battery))
            battery->setText("--");

        if (!fresh(clock->odom)) {
            linear->setText("--");
            angular->setText("--");
            x->setText("X position: -- m");
            y->setText("Y position: -- m");
            yaw->setText("Heading: --");
            manualVelocity->setText("Measured velocity: --");
        }

        if (!fresh(clock->imu)) {
            roll->setText("Roll: --");
            pitch->setText("Pitch: --");
        }
    });

    timer->start(250);

    // Shared system footer
    main->addWidget(label(
        "Wi-Fi UDP  |  ROS Domain 20  |  Standby  |  Unity pending",
        "mutedLabel"));

    return central;
}

void GUIManager::onManualOverridesClicked()
{
    emit manualOverridesRequested();
}

void GUIManager::selectRover(int roverId)
{
    emit roverSelected(roverId);
}

}
