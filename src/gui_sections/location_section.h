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

private:
    void setupLocationLayout();
};

}

#endif