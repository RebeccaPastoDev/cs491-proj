#ifndef SPLASH_SCREEN_H
#define SPLASH_SCREEN_H

#include <QWidget>
#include <QLabel>
#include <QPushButton>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QGroupBox>
#include <QGridLayout>

namespace LunarRoverUI {

class SplashScreen : public QWidget
{
    Q_OBJECT

public:
    explicit SplashScreen(QWidget *parent = nullptr);

signals:
    void roverSelected(int roverId);

private:
    void setupUI();
    void setupRoverSection();
    void setupInfoPanel();

    QLabel *m_logoLabel;
    QGroupBox *m_roverGroup;
    QGroupBox *m_infoGroup;
    QPushButton *m_rover1Button;
    QPushButton *m_rover2Button;
    QLabel *m_rover1Status;
    QLabel *m_rover1Objective;
    QLabel *m_rover2Status;
    QLabel *m_rover2Objective;
    QLabel *m_connectionStatus;
    QLabel *m_batteryLevel;
    QLabel *m_signalStrength;
    QLabel *m_lastUpdate;
};

}

#endif