#ifndef OVERRIDES_SECTION_H
#define OVERRIDES_SECTION_H

#include <QFrame>
#include <QHBoxLayout>
#include <QPushButton>

namespace LunarRoverUI {

class OverridesSection : public QFrame
{
    Q_OBJECT

public:
    explicit OverridesSection(QWidget *parent = nullptr);

signals:
    void manualOverridesRequested();

private:
    void setupOverridesLayout();
};

}

#endif