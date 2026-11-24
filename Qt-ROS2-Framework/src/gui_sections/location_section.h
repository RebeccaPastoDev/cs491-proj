#ifndef LOCATION_SECTION_H
#define LOCATION_SECTION_H

#include <QGroupBox>
#include <QVBoxLayout>
#include <QLabel>

namespace LunarRoverUI {

class LocationSection : public QGroupBox
{
    Q_OBJECT

public:
    explicit LocationSection(QWidget *parent = nullptr);

public slots:
    void updatePosition(double x, double y, double z);
    void updateVelocity(double linear, double angular);

private:
    void setupLocationLayout();

    QLabel *m_xCoordLabel;
    QLabel *m_yCoordLabel;
    QLabel *m_velocityLabel;
};

}

#endif
