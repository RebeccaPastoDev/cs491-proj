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
    QLabel *batteryLabel = new QLabel(tr("Battery: 100%"), this);
    batteryLabel->setObjectName("batteryLabel");

    // connection status
    QLabel *connectionLabel = new QLabel(tr("Connection: Connected to LunarNet"), this);
    connectionLabel->setObjectName("connectionLabel");

    statusLayout->addWidget(batteryLabel);
    statusLayout->addWidget(connectionLabel);
}

}