#ifndef STATUS_SECTION_H
#define STATUS_SECTION_H

#include <QFrame>
#include <QHBoxLayout>
#include <QLabel>

namespace LunarRoverUI {

class StatusSection : public QFrame
{
    Q_OBJECT

public:
    explicit StatusSection(QWidget *parent = nullptr);

private:
    void setupStatusLayout();
};

}

#endif