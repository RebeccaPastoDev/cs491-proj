#include "lidar_section.h"
#include <QPainter>
#include <QTimer>
#include <QPolygonF>
#include <cmath>
#include <algorithm>
#include <limits>

namespace LunarRoverUI {

LidarSection::LidarSection(QWidget *parent)
    : QWidget(parent)
{
    setMinimumHeight(340);
    setSizePolicy(QSizePolicy::Expanding,
                  QSizePolicy::Expanding);

    QTimer *timer = new QTimer(this);
    connect(timer, &QTimer::timeout, this, [this]() {
        update();
    });
    timer->start(250);
}

void LidarSection::updateScan(
    QVector<float> ranges,
    float angleMin,
    float angleIncrement,
    float rangeMin,
    float rangeMax)
{
    m_points.clear();
    m_totalSamples = ranges.size();

    m_nearest = std::numeric_limits<double>::infinity();
    double farthest = 0.0;

    for (int i = 0; i < ranges.size(); ++i) {
        double r = ranges[i];

        if (!std::isfinite(r) ||
            r <= 0 || r < rangeMin || r > rangeMax)
            continue;

        double angle = angleMin + i * angleIncrement;

        if (!std::isfinite(angle))
            continue;

        m_points.append(QPointF(
            -r * std::sin(angle),
            -r * std::cos(angle)
        ));

        m_nearest = std::min(m_nearest, r);
        farthest = std::max(farthest, r);
    }

    m_displayRange = std::max(2.0, std::ceil(farthest));
    m_lastScan.restart();
    update();
}

void LidarSection::paintEvent(QPaintEvent *)
{
    QPainter p(this);
    p.setRenderHint(QPainter::Antialiasing);

    p.fillRect(rect(), QColor("#101b2b"));

    bool live = m_lastScan.isValid() &&
                m_lastScan.elapsed() < 3000;

    p.setPen(QColor("#e2e8f0"));
    p.setFont(QFont("Sans Serif", 12, QFont::Bold));
    p.drawText(20, 28, "LIVE LiDAR");

    p.setFont(QFont("Sans Serif", 10));
    p.setPen(QColor(live ? "#34d399" : "#fbbf24"));

    QString status = live ? "RECEIVING SCANS" :
        (m_lastScan.isValid() ? "DATA STALE" :
                               "WAITING FOR DATA");

    p.drawText(QRect(0, 10, width()-20, 24),
               Qt::AlignRight, status);

    QPointF center(width()/2.0, height()/2.0);
    double radius = std::max(20.0,
        std::min(width()/2.0-35.0,
                 height()/2.0-55.0));

    double scale = radius / m_displayRange;

    p.setPen(QPen(QColor("#26364c"), 1));

    for (int i = 1; i <= 4; ++i) {
        double ring = radius * i / 4.0;
        p.drawEllipse(center, ring, ring);

        p.setPen(QColor("#94a3b8"));
        p.drawText(center + QPointF(6, -ring+12),
            QString("%1 m").arg(
                m_displayRange*i/4.0, 0, 'f', 1));
        p.setPen(QColor("#26364c"));
    }

    p.drawLine(center + QPointF(-radius, 0),
               center + QPointF(radius, 0));

    p.drawLine(center + QPointF(0, -radius),
               center + QPointF(0, radius));

    p.setPen(QColor("#94a3b8"));
    p.drawText(center + QPointF(-24, -radius-8),
               "SCAN +X");

    if (live) {
        p.setPen(QPen(QColor("#34d399"),
                      3, Qt::SolidLine, Qt::RoundCap));

        for (const QPointF &point : m_points)
            p.drawPoint(center + point * scale);
    }

    // Central sensor/rover marker
    p.setPen(QPen(QColor("#60a5fa"), 2));
    p.setBrush(QColor("#162235"));

    QPolygonF rover;
    rover << center + QPointF(0, -10)
          << center + QPointF(-7, 7)
          << center + QPointF(7, 7);

    p.drawPolygon(rover);

    p.setPen(QColor("#94a3b8"));

    QString nearest = "--";

    if (live && std::isfinite(m_nearest))
        nearest = QString("%1 m").arg(
            m_nearest, 0, 'f', 2);

    p.drawText(20, height()-18,
               "Nearest return: " + nearest);

    QString samples = live ?
        QString("%1 / %2 valid samples")
            .arg(m_points.size()).arg(m_totalSamples)
        : QString("No current scan");

    p.drawText(QRect(0, height()-36,
                     width()-20, 24),
               Qt::AlignRight, samples);
}

}
