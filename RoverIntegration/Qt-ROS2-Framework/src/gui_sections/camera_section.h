#ifndef CAMERA_SECTION_H
#define CAMERA_SECTION_H

#include <QGroupBox>
#include <QVBoxLayout>
#include <QLabel>
#include <QTimer>

namespace LunarRoverUI {

class CameraSection : public QGroupBox
{
    Q_OBJECT

public:
    explicit CameraSection(QWidget *parent = nullptr);
    ~CameraSection();

public slots:
    void onCameraFrameReceived(const unsigned char *data, size_t dataSize, int width, int height);

private:
    void setupCameraLayout();

    QLabel *cameraLabel;
};

}

#endif
