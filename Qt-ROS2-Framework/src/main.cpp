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
    // Initialize ROS 2 subscriptions for the rover
    auto bridge = mainWindow.getSimulationBridge();

    if (bridge) {
        if (!bridge->connectToRover("172.20.10.2", 8090)) {
            QMessageBox::warning(
            nullptr,
            "ROS 2 Connection Error",
            "Failed to initialize ROS 2 communication."
        );
    }
}

    mainWindow.show();

    return app.exec();
}
