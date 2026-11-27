#include "mainwindow.h"
#include <QStackedWidget>

namespace LunarRoverUI {

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
{
    setWindowTitle(tr("Lunar Rover | Mission Control"));
    resize(1100, 900);
    setMinimumSize(760, 600);

    m_simulationBridge = new SimulationBridge(this);
    m_guiManager = new GUIManager(m_simulationBridge, this);
    m_splashScreen = new SplashScreen(this);
    m_backButton = nullptr;

    // create stacked widget for screen management
    m_stackedWidget = new QStackedWidget(this);
    setCentralWidget(m_stackedWidget);

    // add splash screen to stacked widget
    m_stackedWidget->addWidget(m_splashScreen);

    // connect rover selection signal
    connect(m_splashScreen, &SplashScreen::roverSelected, this, &MainWindow::onRoverSelected);

    // Show splash screen first
    showSplashScreen();
}

MainWindow::~MainWindow()
{
    delete m_guiManager;
    delete m_splashScreen;
    delete m_stackedWidget;
    delete m_simulationBridge;
}

// Get the simulation bridge for external access
SimulationBridge* MainWindow::getSimulationBridge()
{
    return m_simulationBridge;
}

void MainWindow::showSplashScreen()
{
    m_stackedWidget->setCurrentWidget(m_splashScreen);
}

void MainWindow::showMainUI()
{
    // Reuse the existing dashboard and ROS signal connections.
    if (m_stackedWidget->count() > 1) {
        m_stackedWidget->setCurrentIndex(1);
        return;
    }

    QWidget *uiWidget = m_guiManager->createUI(this);
    m_stackedWidget->addWidget(uiWidget);
    m_stackedWidget->setCurrentWidget(uiWidget);

    // add back button to main window
    if (!m_backButton) {
        m_backButton = new QPushButton(tr("Back to Rover Selection"), this);
        m_backButton->setObjectName("backButton");
        m_backButton->show();
        connect(m_backButton, &QPushButton::clicked, this, &MainWindow::showSplashScreen);
    }

    // add back button to the main layout too
    QVBoxLayout *mainLayout = qobject_cast<QVBoxLayout*>(uiWidget->layout());
    if (mainLayout && mainLayout->indexOf(m_backButton) == -1) {
        mainLayout->addWidget(m_backButton);
    }
}

void MainWindow::onRoverSelected(int roverId)
{
    if (roverId == 1) {
        showMainUI();
    }
}
}
