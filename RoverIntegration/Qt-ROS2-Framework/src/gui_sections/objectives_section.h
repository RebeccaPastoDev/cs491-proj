#ifndef OBJECTIVES_SECTION_H
#define OBJECTIVES_SECTION_H

#include <QGroupBox>
#include <QVBoxLayout>
#include <QLabel>

namespace LunarRoverUI {

class ObjectivesSection : public QGroupBox
{
    Q_OBJECT

public:
    explicit ObjectivesSection(QWidget *parent = nullptr);

private:
    void setupObjectivesLayout();
};

}

#endif