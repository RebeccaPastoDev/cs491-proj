#include "overrides_section.h"

namespace LunarRoverUI {

OverridesSection::OverridesSection(QWidget *parent)
    : QFrame(parent)
{
    setupOverridesLayout();
}

void OverridesSection::setupOverridesLayout()
{
    // set frame properties
    setObjectName("overridesFrame");
    setFrameShape(QFrame::StyledPanel);
    setFrameShadow(QFrame::Raised);

    // create horizontal layout
    QHBoxLayout *overridesLayout = new QHBoxLayout(this);

    // add left spacer
    QSpacerItem *leftSpacer = new QSpacerItem(40, 20, QSizePolicy::Expanding, QSizePolicy::Minimum);
    overridesLayout->addItem(leftSpacer);

    // manual overrides button
    QPushButton *manualOverridesButton = new QPushButton(tr("Open Manual Overrides?"), this);
    manualOverridesButton->setObjectName("manualOverridesButton");
    connect(manualOverridesButton, &QPushButton::clicked, this, &OverridesSection::manualOverridesRequested);

    overridesLayout->addWidget(manualOverridesButton);

    // add right spacer
    QSpacerItem *rightSpacer = new QSpacerItem(40, 20, QSizePolicy::Expanding, QSizePolicy::Minimum);
    overridesLayout->addItem(rightSpacer);
}

}