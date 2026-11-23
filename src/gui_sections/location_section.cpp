#include "location_section.h"

namespace LunarRoverUI {

LocationSection::LocationSection(QWidget *parent)
    : QGroupBox(parent)
{
    setupLocationLayout();
}

void LocationSection::setupLocationLayout()
{
    // set group box properties
    setTitle(tr("Rover Location"));
    setObjectName("locationGroupBox");

    // create vertical layout
    QVBoxLayout *locationLayout = new QVBoxLayout(this);

    // X coordinate
    QLabel *xCoordLabel = new QLabel(tr("X: 1234.56"), this);
    xCoordLabel->setObjectName("xCoordLabel");

    // Y coordinate
    QLabel *yCoordLabel = new QLabel(tr("Y: 7890.12"), this);
    yCoordLabel->setObjectName("yCoordLabel");

    locationLayout->addWidget(xCoordLabel);
    locationLayout->addWidget(yCoordLabel);
}

}