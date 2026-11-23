#include "gui_manager.h"
#include "simulation_bridge.h"
#include "gui_sections/status_section.h"
#include "gui_sections/camera_section.h"
#include "gui_sections/objectives_section.h"
#include "gui_sections/location_section.h"
#include "gui_sections/overrides_section.h"

namespace LunarRoverUI {

GUIManager::GUIManager(QObject *parent)
    : QObject(parent)
{
    m_simulationBridge = new SimulationBridge(this);
}

QWidget* GUIManager::createUI(QWidget *parent)
{
    QWidget *centralWidget = new QWidget(parent);

    // create vertical layout
    QVBoxLayout *mainLayout = new QVBoxLayout(centralWidget);
    mainLayout->setMargin(10);
    mainLayout->setSpacing(15);

    // setup all UI sections
    m_statusSection = new StatusSection(centralWidget);
    mainLayout->addWidget(m_statusSection);

    m_cameraSection = new CameraSection(centralWidget);
    mainLayout->addWidget(m_cameraSection);

    // Wire up simulation bridge signals to UI sections
    connect(m_simulationBridge, &SimulationBridge::cameraFrameReceived,
            m_cameraSection, &CameraSection::onCameraFrameReceived);

    m_objectivesSection = new ObjectivesSection(centralWidget);
    mainLayout->addWidget(m_objectivesSection);

    m_locationSection = new LocationSection(centralWidget);
    mainLayout->addWidget(m_locationSection);

    m_overridesSection = new OverridesSection(centralWidget);
    mainLayout->addWidget(m_overridesSection);

    // add spacer
    QSpacerItem *verticalSpacer = new QSpacerItem(20, 40, QSizePolicy::Minimum, QSizePolicy::Expanding);
    mainLayout->addItem(verticalSpacer);

    return centralWidget;
}

void GUIManager::onManualOverridesClicked()
{
    emit manualOverridesRequested();
}

void GUIManager::selectRover(int roverId)
{
    emit roverSelected(roverId);
}

}
