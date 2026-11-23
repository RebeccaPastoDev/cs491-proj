#include "splash_screen.h"
#include <QApplication>
#include <QFontDatabase>
#include <QStyleFactory>

namespace LunarRoverUI {

SplashScreen::SplashScreen(QWidget *parent)
    : QWidget(parent)
{
    setupUI();
}

void SplashScreen::setupUI()
{
    // set window properties
    setWindowTitle(tr("Lunar Rover UI - Select Rover"));
    resize(800, 600);

    // main layout
    QVBoxLayout *mainLayout = new QVBoxLayout(this);
    mainLayout->setMargin(20);
    mainLayout->setSpacing(20);

    // logo section
    m_logoLabel = new QLabel(this);
    m_logoLabel->setText(tr("Lunar Rover UI"));
    QFont logoFont = QFontDatabase::systemFont(QFontDatabase::GeneralFont);
    logoFont.setPointSize(24);
    logoFont.setBold(true);
    m_logoLabel->setFont(logoFont);
    m_logoLabel->setAlignment(Qt::AlignCenter);
    mainLayout->addWidget(m_logoLabel);

    // create rover and info sections
    setupRoverSection();
    setupInfoPanel();

    // add sections to main layout
    mainLayout->addWidget(m_roverGroup);
    mainLayout->addWidget(m_infoGroup);

    // set layout
    setLayout(mainLayout);
}

void SplashScreen::setupRoverSection()
{
    m_roverGroup = new QGroupBox(tr("Select Rover"), this);
    QGridLayout *roverLayout = new QGridLayout(m_roverGroup);

// Rover 1
m_rover1Button = new QPushButton(tr("Rover 1"), this);
m_rover1Button->setSizePolicy(QSizePolicy::Expanding, QSizePolicy::Preferred);

m_rover1Status = new QLabel(tr("Connected"), this);
m_rover1Status->setObjectName("rover1Status");
m_rover1Objective = new QLabel(tr("Exploring Crater Region"), this);

// Rover 2
m_rover2Button = new QPushButton(tr("Rover 2"), this);
m_rover2Button->setSizePolicy(QSizePolicy::Expanding, QSizePolicy::Preferred);
m_rover2Button->setEnabled(false);

m_rover2Status = new QLabel(tr("Not Connected"), this);
m_rover2Status->setObjectName("rover2Status");
m_rover2Objective = new QLabel(tr("Standby"), this);

// add widgets to layout
roverLayout->addWidget(m_rover1Button, 0, 0);
roverLayout->addWidget(m_rover1Status, 1, 0);
roverLayout->addWidget(m_rover1Objective, 2, 0);
roverLayout->addWidget(m_rover2Button, 0, 1);
roverLayout->addWidget(m_rover2Status, 1, 1);
roverLayout->addWidget(m_rover2Objective, 2, 1);

// connect signals
connect(m_rover1Button, &QPushButton::clicked, this, [this]() {
    emit roverSelected(1);
});
}

void SplashScreen::setupInfoPanel()
{
    m_infoGroup = new QGroupBox(tr("System Information"), this);
    QGridLayout *infoLayout = new QGridLayout(m_infoGroup);

    // connection status
    QLabel *connectionLabel = new QLabel(tr("Connection Status:"), this);
    m_connectionStatus = new QLabel(tr("Connected"), this);
    m_connectionStatus->setObjectName("connectionStatus");

    // battery level
    QLabel *batteryLabel = new QLabel(tr("Battery Level:"), this);
    m_batteryLevel = new QLabel(tr("95%"), this);

    // signal strength
    QLabel *signalLabel = new QLabel(tr("Signal Strength:"), this);
    m_signalStrength = new QLabel(tr("Excellent"), this);

    // last update
    QLabel *updateLabel = new QLabel(tr("Last Update:"), this);
    m_lastUpdate = new QLabel(tr("Just now"), this);

    // add widgets to layout
    infoLayout->addWidget(connectionLabel, 0, 0);
    infoLayout->addWidget(m_connectionStatus, 0, 1);
    infoLayout->addWidget(batteryLabel, 1, 0);
    infoLayout->addWidget(m_batteryLevel, 1, 1);
    infoLayout->addWidget(signalLabel, 2, 0);
    infoLayout->addWidget(m_signalStrength, 2, 1);
    infoLayout->addWidget(updateLabel, 3, 0);
    infoLayout->addWidget(m_lastUpdate, 3, 1);

    // set layout
    m_infoGroup->setLayout(infoLayout);
}

}