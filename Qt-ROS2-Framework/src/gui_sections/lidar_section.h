#ifndef LIDAR_SECTION_H
#define LIDAR_SECTION_H

#include <QWidget>
#include <QVector>
#include <QPointF>
#include <QElapsedTimer>

namespace LunarRoverUI {

class LidarSection : public QWidget
{
    Q_OBJECT

public:
    explicit LidarSection(QWidget *parent = nullptr);

public slots:
    void updateScan(QVector<float> ranges,
                    float angleMin,
                    float angleIncrement,
                    float rangeMin,
                    float rangeMax);

protected:
    void paintEvent(QPaintEvent *event) override;

private:
    QVector<QPointF> m_points;
    QElapsedTimer m_lastScan;
    double m_nearest = 0.0;
    double m_displayRange = 2.0;
    int m_totalSamples = 0;
};

}

#endif
