#include <QApplication>
#include <QFile>
#include <QTextStream>
#include <QMessageBox>
#include "mainwindow.h"

QString loadStyleSheet(const QString& filename)
{
    QFile file(filename);
    if (!file.open(QFile::ReadOnly | QFile::Text)) {
        return QString();
    }

    QTextStream stream(&file);
    QString stylesheet = stream.readAll();
    file.close();
    return stylesheet;
}

int main(int argc, char *argv[])
{
    QApplication app(argc, argv);

    // load stylesheet
    QString stylesheet = loadStyleSheet(":/styles.qss");
    if (!stylesheet.isEmpty()) {
        app.setStyleSheet(stylesheet);
    }

    LunarRoverUI::MainWindow mainWindow;
    // Initialize micro-ROS connection
    auto bridge = mainWindow.getSimulationBridge();
    if (bridge) {
        // Try to connect to micro-ROS
        if (!bridge->connectToMicroROS("your_wifi_ssid", "your_wifi_password",
                                      "192.168.1.100", 8090)) {
            QMessageBox::warning(nullptr, "Connection Error",
                               "Failed to connect to Micro-ROS agent. "
                               "Please check your WiFi credentials and agent IP.");
        }
    }

    mainWindow.show();

    return app.exec();
}
