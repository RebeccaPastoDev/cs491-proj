#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QStackedWidget>
#include "gui_manager.h"
#include "splash_screen.h"

namespace LunarRoverUI {

class MainWindow : public QMainWindow
{
    Q_OBJECT

public:
    MainWindow(QWidget *parent = nullptr);
    ~MainWindow();

    // Get the simulation bridge for external access
    SimulationBridge* getSimulationBridge();

private slots:
    void onRoverSelected(int roverId);

private:
    void showSplashScreen();
    void showMainUI();

private:
    GUIManager *m_guiManager;
    SplashScreen *m_splashScreen;
    QPushButton *m_backButton;
    QStackedWidget *m_stackedWidget;
    SimulationBridge *m_simulationBridge;
};

}

#endif