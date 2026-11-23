#include "status_section.h"
#include <QFont>

namespace LunarRoverUI {

StatusSection::StatusSection(QWidget *parent)
    : QFrame(parent)
{
    setupStatusLayout();
}

void StatusSection::setupStatusLayout()
{
    // set frame properties
    setObjectName("statusFrame");
    setFrameShape(QFrame::StyledPanel);
    setFrameShadow(QFrame::Raised);

    // create horizontal layout
    QHBoxLayout *statusLayout = new QHBoxLayout(this);
    statusLayout->setSpacing(20);

    // battery status
    m_batteryLabel = new QLabel(tr("Battery: -- (waiting for data)"), this);
    m_batteryLabel->setObjectName("batteryLabel");

    // connection status
    QLabel *connectionLabel = new QLabel(tr("Connection: Connected to LunarNet"), this);
    connectionLabel->setObjectName("connectionLabel");

    statusLayout->addWidget(m_batteryLabel);
    statusLayout->addWidget(connectionLabel);
}

void StatusSection::updateBattery(double value)
{
    m_batteryLabel->setText(
        QString("Battery (raw): %1").arg(value, 0, 'f', 0)
    );
}

}
