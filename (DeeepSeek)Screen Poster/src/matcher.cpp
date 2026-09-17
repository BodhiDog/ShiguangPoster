#include "matcher.h"
#include <QImage>
#include <QColor>
#include <cmath>
#include <algorithm>

Tone Matcher::analyzeImage(const QString& path) {
    Tone tone;
    QImage img(path);
    if (img.isNull()) return tone;
    int w = img.width(), h = img.height();
    if (w <= 0 || h <= 0) return tone;

    img = img.convertToFormat(QImage::Format_RGB32);
    double sumR = 0, sumG = 0, sumB = 0;
    long count = 0;
    int grid = 9;
    for (int gy = 0; gy < grid; ++gy) {
        for (int gx = 0; gx < grid; ++gx) {
            int x = gx * w / grid;
            int y = gy * h / grid;
            QColor c = img.pixelColor(x, y);
            sumR += c.red(); sumG += c.green(); sumB += c.blue();
            ++count;
        }
    }
    if (count == 0) return tone;
    double r = sumR / count, g = sumG / count, b = sumB / count;
    tone.bright = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
    tone.warm = std::clamp((r - b + 255.0) / 510.0, 0.0, 1.0);
    return tone;
}

namespace {
struct Kw {
    const char* word;
    double warm;
    double bright;
};

const Kw g_kw[] = {
    {"阳光", 0.4, 0.4}, {"希望", 0.3, 0.3}, {"奋斗", 0.2, 0.3}, {"梦想", 0.3, 0.3},
    {"热情", 0.4, 0.3}, {"光芒", 0.3, 0.4}, {"春天", 0.3, 0.3}, {"温暖", 0.4, 0.2},
    {"乐观", 0.2, 0.3}, {"快乐", 0.3, 0.3}, {"微笑", 0.3, 0.3}, {"成功", 0.2, 0.3},
    {"明天", 0.2, 0.3}, {"力量", 0.2, 0.3}, {"幸福", 0.3, 0.3}, {"友谊", 0.3, 0.1},
    {"爱", 0.3, 0.2}, {"家", 0.3, 0.0}, {"青春", 0.2, 0.3}, {"生命", 0.2, 0.2},
    {"夜", -0.3, -0.4}, {"月", -0.2, -0.3}, {"孤独", -0.2, -0.3}, {"忧伤", -0.1, -0.3},
    {"雨", -0.2, -0.3}, {"寒", -0.4, -0.2}, {"沉默", -0.1, -0.2}, {"思念", -0.1, -0.2},
    {"冬", -0.4, -0.3}, {"秋", -0.2, -0.2}, {"悲伤", -0.1, -0.3}, {"泪", -0.1, -0.3},
    {"雪", -0.4, 0.2}, {"星", -0.2, 0.1}, {"海", -0.3, 0.0}, {"天空", -0.2, 0.3},
    {"宁静", -0.1, -0.1}, {"静", -0.1, -0.1}, {"平和", 0.0, -0.1}, {"淡然", 0.0, -0.1},
    {"从容", 0.0, -0.1}, {"岁月", 0.0, -0.1}, {"远方", 0.1, 0.1}, {"自由", 0.2, 0.2},
};
}

Mood Matcher::analyzeQuote(const std::string& quote) {
    Mood mood;
    double warm = 0.5, bright = 0.5;
    for (auto& k : g_kw) {
        if (quote.find(k.word) != std::string::npos) {
            warm += k.warm;
            bright += k.bright;
        }
    }
    mood.warm = std::clamp(warm, 0.0, 1.0);
    mood.bright = std::clamp(bright, 0.0, 1.0);
    return mood;
}

double Matcher::score(const Tone& t, const Mood& m) const {
    double diff = (std::fabs(t.bright - m.bright) + std::fabs(t.warm - m.warm)) / 2.0;
    return std::clamp(1.0 - diff, 0.0, 1.0);
}
