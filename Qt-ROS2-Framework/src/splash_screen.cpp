#include "splash_screen.h"
#include <QFrame>

namespace LunarRoverUI {

SplashScreen::SplashScreen(QWidget *parent)
    : QWidget(parent)
{
    setupUI();
}

void SplashScreen::setupUI()
{
    auto *layout = new QVBoxLayout(this);
    layout->setContentsMargins(36, 32, 36, 24);
    layout->setSpacing(20);

    auto *tag = new QLabel("MISSION CONTROL SYSTEM");
    tag->setStyleSheet(
        "color: #60a5fa; font-weight: bold;"
        "letter-spacing: 2px;");

    layout->addWidget(tag);

    m_logoLabel = new QLabel("LUNAR ROVER");
    m_logoLabel->setObjectName("pageTitle");
    m_logoLabel->setStyleSheet("font-size: 32pt;");
    layout->addWidget(m_logoLabel);


    layout->addSpacing(12);

    setupRoverSection();
    setupInfoPanel();

    layout->addWidget(m_roverGroup);
    layout->addWidget(m_infoGroup);
    layout->addStretch();

}

void SplashScreen::setupRoverSection()
{
    m_roverGroup = new QGroupBox("SELECT ROVER");
    auto *row = new QHBoxLayout(m_roverGroup);
    row->setSpacing(16);

    // Rover 01
    auto *rover1 = new QFrame;
    rover1->setObjectName("card");

    auto *first = new QVBoxLayout(rover1);
    first->setContentsMargins(20, 20, 20, 20);
    first->setSpacing(12);

    m_rover1Status = new QLabel("CONFIGURED");
    m_rover1Status->setObjectName("successLabel");
    first->addWidget(m_rover1Status);

    auto *title1 = new QLabel("ROVER 01");
    title1->setObjectName("pageTitle");
    first->addWidget(title1);

    m_rover1Objective = new QLabel(
        "Yahboom ESP32-S3\nPhysical rover");
    m_rover1Objective->setObjectName("mutedLabel");
    first->addWidget(m_rover1Objective);

    first->addStretch();

    m_rover1Button = new QPushButton("Open Dashboard");
    m_rover1Button->setObjectName("primaryButton");
    m_rover1Button->setMinimumHeight(44);
    first->addWidget(m_rover1Button);

    // Rover 02
    auto *rover2 = new QFrame;
    rover2->setObjectName("card");

    auto *second = new QVBoxLayout(rover2);
    second->setContentsMargins(20, 20, 20, 20);
    second->setSpacing(12);

    m_rover2Status = new QLabel("NOT CONFIGURED");
    m_rover2Status->setObjectName("mutedLabel");
    second->addWidget(m_rover2Status);

    auto *title2 = new QLabel("ROVER 02");
    title2->setObjectName("pageTitle");
    title2->setStyleSheet("color: #64748b;");
    second->addWidget(title2);

    m_rover2Objective = new QLabel(
        "Future expansion\nNo rover assigned");
    m_rover2Objective->setObjectName("mutedLabel");
    second->addWidget(m_rover2Objective);

    second->addStretch();

    m_rover2Button = new QPushButton("Unavailable");
    m_rover2Button->setMinimumHeight(44);
    m_rover2Button->setEnabled(false);
    second->addWidget(m_rover2Button);

    row->addWidget(rover1, 1);
    row->addWidget(rover2, 1);

    connect(m_rover1Button, &QPushButton::clicked,
            this, [this]() {
        emit roverSelected(1);
    });
}

void SplashScreen::setupInfoPanel()
{
    m_infoGroup = new QGroupBox("SYSTEM CONFIGURATION");

    auto *grid = new QGridLayout(m_infoGroup);
    grid->setHorizontalSpacing(28);
    grid->setVerticalSpacing(14);

    auto addRow = [grid](int row,
                        const QString &name,
                        const QString &value) {
        auto *key = new QLabel(name);
        key->setObjectName("mutedLabel");

        auto *val = new QLabel(value);

        grid->addWidget(key, row, 0);
        grid->addWidget(val, row, 1);
    };

    addRow(0, "ROS distribution", "ROS 2 Humble");
    addRow(1, "Transport", "micro-ROS Wi-Fi UDP");
    addRow(2, "ROS domain", "20");
    addRow(3, "Agent endpoint", "172.20.10.2:8090");
    addRow(4, "Unity integration", "Pending");
    addRow(5, "Motion control", "Safety locked");

    grid->setColumnStretch(1, 1);

    auto *note = new QLabel(
        "Live sensor status is available in the dashboard.");
    note->setObjectName("mutedLabel");
    note->setWordWrap(true);

    grid->addWidget(note, 6, 0, 1, 2);
}

}
