#include "camera_section.h"

#include <QPixmap>
#include <QImage>
#include <QBuffer>

namespace LunarRoverUI {

CameraSection::CameraSection(QWidget *parent)
    : QGroupBox(parent)
{
    setupCameraLayout();
    cameraLabel->setText(tr("Waiting for camera feed..."));
}

CameraSection::~CameraSection()
{
}

void CameraSection::onCameraFrameReceived(const unsigned char *data, size_t dataSize, int width, int height)
{
    // Yahboom sends JPEG compressed images via /esp32_img
    if (data == nullptr || dataSize == 0) {
        cameraLabel->setText(tr("Camera feed disconnected"));
        return;
    }

    // Decode JPEG from raw buffer using QImage
    QImage image;
    bool loaded = image.loadFromData(data, static_cast<int>(dataSize), "JPEG");
    
    if (!loaded) {
        // Try auto-detect format if JPEG fails
        loaded = image.loadFromData(data, static_cast<int>(dataSize));
    }

    if (loaded && !image.isNull()) {
        QPixmap pixmap = QPixmap::fromImage(image).scaled(
            cameraLabel->size(), Qt::KeepAspectRatio, Qt::SmoothTransformation);
        cameraLabel->setPixmap(pixmap);
    } else {
        cameraLabel->setText(tr("⚠️ Failed to decode camera frame"));
    }
}

void CameraSection::setupCameraLayout()
{
    // set group box properties
    setTitle(tr("Camera Feed"));
    setObjectName("cameraGroupBox");

    // create vertical layout
    QVBoxLayout *cameraLayout = new QVBoxLayout(this);

    // camera display label
    cameraLabel = new QLabel(this);
    cameraLabel->setObjectName("cameraDisplay");
    cameraLabel->setAlignment(Qt::AlignCenter);
    cameraLabel->setMinimumSize(320, 240);
    cameraLabel->setText(tr("Initializing camera..."));

    cameraLayout->addWidget(cameraLabel);
}

}
