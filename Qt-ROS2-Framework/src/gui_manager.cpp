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

    // Manual override page - no hardware actuation
    auto *manual = new QWidget;
    auto *ml = new QVBoxLayout(manual);

    ml->addWidget(label("MANUAL OVERRIDE", "pageTitle"));

    auto *warning = label(
        "SAFETY LOCKED - Physical movement disabled",
        "warningLabel");

    warning->setWordWrap(true);
    ml->addWidget(warning);

    auto *controls = card(ml, "DRIVE CONTROLS");
    auto *dpad = new QGridLayout;

    auto driveButton = [](const QString &name) {
        auto *b = new QPushButton(name);
        b->setMinimumSize(100, 54);
        b->setEnabled(false);
        return b;
    };

    dpad->addWidget(driveButton("Forward"), 0, 1);
    dpad->addWidget(driveButton("Left"), 1, 0);
    dpad->addWidget(driveButton("Stop"), 1, 1);
    dpad->addWidget(driveButton("Right"), 1, 2);
    dpad->addWidget(driveButton("Reverse"), 2, 1);

    controls->addLayout(dpad);

    auto *speed = card(ml, "TARGET SPEED - PREVIEW ONLY");

    auto *linearTarget = label("Linear target: 0.00 m/s");
    auto *angularTarget = label("Angular target: 0.00 rad/s");

    auto *linearSlider = new QSlider(Qt::Horizontal);
    linearSlider->setRange(0, 50);

    auto *angularSlider = new QSlider(Qt::Horizontal);
    angularSlider->setRange(0, 200);

    speed->addWidget(linearTarget);
    speed->addWidget(linearSlider);
    speed->addWidget(angularTarget);
    speed->addWidget(angularSlider);

    connect(linearSlider, &QSlider::valueChanged,
            linearTarget, [linearTarget](int v) {
        linearTarget->setText(
            QString("Linear target: %1 m/s")
                .arg(v / 100.0, 0, 'f', 2));
    });

    connect(angularSlider, &QSlider::valueChanged,
            angularTarget, [angularTarget](int v) {
        angularTarget->setText(
            QString("Angular target: %1 rad/s")
                .arg(v / 100.0, 0, 'f', 2));
    });

    auto *safety = card(ml, "SAFETY STATUS");
    auto *arm = new QPushButton("Arm Drive - Unavailable");
    arm->setEnabled(false);

    auto *manualVelocity = label("Measured velocity: --");

    safety->addWidget(manualVelocity);
    safety->addWidget(label("Control mode: Standby"));
    safety->addWidget(label("Motor watchdog: Not verified"));
    safety->addWidget(label("No motion commands transmitted"));
    safety->addWidget(arm);

    ml->addStretch();

    auto *manualScroll = new QScrollArea;
    manualScroll->setWidgetResizable(true);
    manualScroll->setWidget(manual);
    pages->addWidget(manualScroll);

    connect(dashboardButton, &QPushButton::clicked,
            pages, [pages]() {
        pages->setCurrentIndex(0);
    });

    connect(manualButton, &QPushButton::clicked,
            pages, [pages]() {
        pages->setCurrentIndex(1);
    });

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
