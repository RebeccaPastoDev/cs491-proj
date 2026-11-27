#include "location_section.h"

namespace LunarRoverUI {

LocationSection::LocationSection(QWidget *parent)
    : QGroupBox(parent)
{
    setupLocationLayout();
}

void LocationSection::setupLocationLayout()
{
    setTitle(tr("Rover Location"));
    setObjectName("locationGroupBox");

    QVBoxLayout *locationLayout = new QVBoxLayout(this);

    m_xCoordLabel = new QLabel(tr("X: -- m"), this);
    m_xCoordLabel->setObjectName("xCoordLabel");

    m_yCoordLabel = new QLabel(tr("Y: -- m"), this);
    m_yCoordLabel->setObjectName("yCoordLabel");

    m_velocityLabel = new QLabel(tr("Linear velocity X: -- m/s"), this);
    m_velocityLabel->setObjectName("velocityLabel");

    locationLayout->addWidget(m_xCoordLabel);
    locationLayout->addWidget(m_yCoordLabel);
    locationLayout->addWidget(m_velocityLabel);
}

void LocationSection::updatePosition(double x, double y, double z)
{
    Q_UNUSED(z);

    m_xCoordLabel->setText(tr("X: %1 m").arg(x, 0, 'f', 3));
    m_yCoordLabel->setText(tr("Y: %1 m").arg(y, 0, 'f', 3));
}

void LocationSection::updateVelocity(double linear, double angular)
{
    Q_UNUSED(angular);

    m_velocityLabel->setText(
        tr("Linear velocity X: %1 m/s").arg(linear, 0, 'f', 3)
    );
}

}
