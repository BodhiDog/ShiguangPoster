#include "screensaver.h"
#include "util.h"
#include <QPainter>
#include <QPainterPath>
#include <QMouseEvent>
#include <QKeyEvent>
#include <QScreen>
#include <QGuiApplication>
#include <QDateTime>
#include <QFontMetricsF>
#include <cmath>

namespace {
const qreal kPi = 3.14159265358979323846;

float easeInOut(float t) {
    t = qBound(0.0f, t, 1.0f);
    return t * t * (3.0f - 2.0f * t);
}

void drawCover(QPainter& p, const QImage& img, const QRectF& dst) {
    if (img.isNull()) return;
    qreal sw = img.width(), sh = img.height();
    if (sw <= 0 || sh <= 0) return;
    qreal scale = qMax(dst.width() / sw, dst.height() / sh);
    qreal w = sw * scale, h = sh * scale;
    qreal x = dst.x() + (dst.width() - w) / 2.0;
    qreal y = dst.y() + (dst.height() - h) / 2.0;
    p.drawImage(QRectF(x, y, w, h), img);
}

void drawArrow(QPainter& p, qreal cx, qreal cy, qreal half, const QColor& c, bool left) {
    QPolygonF poly;
    if (left) {
        poly << QPointF(cx + half, cy - half) << QPointF(cx + half, cy + half) << QPointF(cx - half, cy);
    } else {
        poly << QPointF(cx - half, cy - half) << QPointF(cx - half, cy + half) << QPointF(cx + half, cy);
    }
    p.setPen(Qt::NoPen);
    p.setBrush(c);
    p.drawPolygon(poly);
}

void drawGear(QPainter& p, const QRect& r, const QColor& bg) {
    qreal cx = r.center().x(), cy = r.center().y();
    qreal R = qMin(r.width(), r.height()) / 2.0;
    p.setPen(Qt::NoPen);
    p.setBrush(QColor(255, 255, 255, 230));
    int teeth = 8;
    for (int i = 0; i < teeth; ++i) {
        qreal a = i * 2.0 * kPi / teeth;
        qreal tx = cx + std::cos(a) * R * 0.82;
        qreal ty = cy + std::sin(a) * R * 0.82;
        p.drawEllipse(QPointF(tx, ty), R * 0.22, R * 0.22);
    }
    p.drawEllipse(QPointF(cx, cy), R * 0.72, R * 0.72);
    p.setBrush(bg);
    p.drawEllipse(QPointF(cx, cy), R * 0.36, R * 0.36);
}

void drawHeart(QPainter& p, const QRect& r, bool filled) {
    qreal cx = r.center().x(), cy = r.center().y();
    qreal s = qMin(r.width(), r.height()) / 2.0;
    auto P = [&](qreal x, qreal y) { return QPointF(cx + x * s, cy + y * s); };
    QPainterPath path;
    path.moveTo(P(0.0, 0.45));
    path.cubicTo(P(-0.45, 0.10), P(-0.50, -0.25), P(-0.28, -0.40));
    path.cubicTo(P(-0.10, -0.52), P(0.0, -0.52), P(0.0, -0.30));
    path.cubicTo(P(0.0, -0.52), P(0.10, -0.52), P(0.28, -0.40));
    path.cubicTo(P(0.50, -0.25), P(0.45, 0.10), P(0.0, 0.45));
    path.closeSubpath();
    if (filled) {
        p.setPen(Qt::NoPen);
        p.setBrush(QColor(255, 224, 60, 60));
        p.drawPath(path);
    } else {
        p.setPen(QPen(Qt::white, qMax(1.5, s * 0.09)));
        p.setBrush(Qt::NoBrush);
        p.drawPath(path);
    }
}
}

ScreensaverController::ScreensaverController(Config& cfg, ImageGallery& gallery, SentenceStore& sentences)
    : cfg_(cfg), gallery_(gallery), sentences_(sentences) {
    connect(&slideTimer_, &QTimer::timeout, this, &ScreensaverController::onSlide);
    connect(&frameTimer_, &QTimer::timeout, this, &ScreensaverController::onFrame);
    connect(&clockTimer_, &QTimer::timeout, this, &ScreensaverController::onClock);
    frameTimer_.setInterval(16);
    clockTimer_.setInterval(1000);
}

ScreensaverController::~ScreensaverController() {
    stop();
}

void ScreensaverController::start() {
    for (QScreen* s : QGuiApplication::screens()) {
        ScreenWidget* w = new ScreenWidget(this, s);
        connect(this, &ScreensaverController::stateChanged, w, [w] { w->update(); });
        w->show();
        w->setFocus();
        screens_.append(w);
    }
    curIndex_ = gallery_.empty() ? 0 : (int)gallery_.pickRandom();
    loadCurrent();
    pickQuote();
    slideTimer_.start(cfg_.slide_interval_seconds * 1000);
    clockTimer_.start();
    emit stateChanged();
}

void ScreensaverController::stop() {
    slideTimer_.stop();
    frameTimer_.stop();
    clockTimer_.stop();
    for (auto& wp : screens_) if (wp) wp->close();
    screens_.clear();
    curImage_ = QImage();
    nextImage_ = QImage();
    transitioning_ = false;
}

void ScreensaverController::loadCurrent() {
    curImage_ = QImage();
    if (!gallery_.empty()) curImage_ = QImage(gallery_.at((size_t)curIndex_).path);
}

void ScreensaverController::pickQuote() {
    quoteIndex_ = -1;
    if (sentences_.size() == 0 || gallery_.empty()) return;
    const std::string& name = gallery_.at((size_t)curIndex_).name;
    int q = sentences_.pickForImage(name);
    if (q < 0) q = sentences_.pickRandom();
    quoteIndex_ = q;
}

void ScreensaverController::switchTo(int idx) {
    if (gallery_.empty()) return;
    int n = (int)gallery_.size();
    idx = ((idx % n) + n) % n;

    if (idx == curIndex_ && !transitioning_) {
        curImage_ = QImage(gallery_.at((size_t)idx).path);
        pickQuote();
        emit stateChanged();
        return;
    }

    nextImage_ = QImage(gallery_.at((size_t)idx).path);
    nextIndex_ = idx;

    if (cfg_.transition_effect == "none") {
        curImage_ = nextImage_;
        nextImage_ = QImage();
        curIndex_ = idx;
        transitioning_ = false;
        pickQuote();
        emit stateChanged();
        return;
    }

    transitioning_ = true;
    progress_ = 0.0f;
    transStart_ = QDateTime::currentMSecsSinceEpoch();
    frameTimer_.start();
    emit stateChanged();
}

void ScreensaverController::finishTransition() {
    transitioning_ = false;
    frameTimer_.stop();
    curIndex_ = nextIndex_;
    curImage_ = nextImage_;
    nextImage_ = QImage();
    pickQuote();
    emit stateChanged();
}

void ScreensaverController::onFrame() {
    qint64 now = QDateTime::currentMSecsSinceEpoch();
    progress_ = (float)(now - transStart_) / (float)qMax(cfg_.transition_duration_ms, 1);
    if (progress_ >= 1.0f) finishTransition();
    else emit stateChanged();
}

void ScreensaverController::onSlide() {
    if (transitioning_ || gallery_.empty()) return;
    size_t nx = gallery_.pickRandom();
    if (gallery_.size() > 1 && nx == (size_t)curIndex_) nx = (nx + 1) % gallery_.size();
    switchTo((int)nx);
}

void ScreensaverController::onClock() {
    emit stateChanged();
}

void ScreensaverController::toggleLike() {
    if (!gallery_.empty() && (size_t)curIndex_ < gallery_.size()) {
        gallery_.toggleLike((size_t)curIndex_);
        emit stateChanged();
    }
}

void ScreensaverController::previous() {
    if (gallery_.size() > 1) switchTo(curIndex_ - 1);
}

void ScreensaverController::next() {
    if (gallery_.size() > 1) switchTo(curIndex_ + 1);
}

void ScreensaverController::setHoverArrow(int h) {
    if (hoverArrow_ != h) { hoverArrow_ = h; emit stateChanged(); }
}

void ScreensaverController::setArrowsVisible(bool v) {
    if (arrowsVisible_ != v) { arrowsVisible_ = v; emit stateChanged(); }
}

const Sentence* ScreensaverController::currentSentence() const {
    if (quoteIndex_ < 0 || (size_t)quoteIndex_ >= sentences_.size()) return nullptr;
    return &sentences_.at((size_t)quoteIndex_);
}

ScreenWidget::ScreenWidget(ScreensaverController* ctl, QScreen* screen)
    : QWidget(nullptr), ctl_(ctl) {
    setWindowFlags(Qt::FramelessWindowHint | Qt::WindowStaysOnTopHint | Qt::Tool);
    setAttribute(Qt::WA_DeleteOnClose);
    setMouseTracking(true);
    setFocusPolicy(Qt::StrongFocus);
    setCursor(Qt::ArrowCursor);
    if (screen) setGeometry(screen->geometry());
    else setGeometry(0, 0, 1920, 1080);
    like1_ = QPixmap(pathJoin(resDir(), "like1.png"));
    like2_ = QPixmap(pathJoin(resDir(), "like2.png"));
    updateRects();
}

void ScreenWidget::updateRects() {
    likeRect_ = QRect(width() - 164, 24, 52, 52);
    settingsRect_ = QRect(width() - 72, 24, 52, 52);
    leftArrow_ = QRect(28, height() / 2 - 64, 68, 128);
    rightArrow_ = QRect(width() - 96, height() / 2 - 64, 68, 128);
}

void ScreenWidget::resizeEvent(QResizeEvent*) {
    updateRects();
}

void ScreenWidget::paintEvent(QPaintEvent*) {
    QPainter p(this);
    p.setRenderHint(QPainter::Antialiasing);
    p.setRenderHint(QPainter::SmoothPixmapTransform);
    QRectF full = rect();
    p.fillRect(full, Qt::black);

    const Config& cfg = ctl_->config();
    const QImage& cur = ctl_->currentImage();
    const QImage& next = ctl_->nextImage();

    if (ctl_->transitioning() && !next.isNull()) {
        float pr = ctl_->progress();
        drawCover(p, cur, full);
        QString fx = QString::fromUtf8(cfg.transition_effect.c_str());
        if (fx == "fade") {
            p.save();
            p.setOpacity(pr);
            drawCover(p, next, full);
            p.restore();
        } else if (fx == "blinds") {
            int N = 14;
            for (int i = 0; i < N; ++i) {
                float local = easeInOut(qBound(0.0f, pr * N - i, 1.0f));
                if (local <= 0.0f) continue;
                qreal y0 = height() * i / (qreal)N;
                qreal sh = height() / (qreal)N;
                p.save();
                p.setClipRect(QRectF(0, y0, width(), sh * local));
                drawCover(p, next, full);
                p.restore();
            }
        } else if (fx == "nonlinear") {
            float e = easeInOut(pr);
            p.save();
            p.setClipRect(QRectF(0, 0, width() * e, height()));
            drawCover(p, next, full);
            p.restore();
        } else {
            drawCover(p, next, full);
        }
    } else if (!cur.isNull()) {
        drawCover(p, cur, full);
    }

    if (ctl_->gallery().empty()) {
        p.setPen(Qt::white);
        QFont f(QString::fromUtf8(cfg.font_name.c_str()));
        f.setPixelSize(40);
        p.setFont(f);
        p.drawText(full, Qt::AlignCenter, QStringLiteral("请将图片放入 pictures 目录"));
    }

    float dtY = -1.0f, qY = -1.0f;
    if (cfg.show_datetime && cfg.show_quote && cfg.datetime_vpos == cfg.quote_vpos) {
        float dtH = (float)cfg.datetime_font_size * 1.6f + 16.0f;
        qreal qbw;
        float qbh;
        const Sentence* s = ctl_->currentSentence();
        if (s) {
            QFont qf(QString::fromUtf8(cfg.font_name.c_str()));
            qf.setPixelSize(cfg.quote_font_size);
            QFontMetricsF qfm(qf);
            QString quote = QString::fromUtf8(s->quote.c_str());
            QString author = s->author.empty() ? QString() : (QStringLiteral("—— ") + QString::fromUtf8(s->author.c_str()));
            float qs = (float)cfg.quote_font_size;
            float as_ = (float)cfg.quote_author_font_size;
            qreal qh = qs * 1.6, ah = as_ * 1.5;
            qreal qw = qfm.horizontalAdvance(quote);
            qreal aw = author.isEmpty() ? 0 : qfm.horizontalAdvance(author);
            float gap = qs * 0.4f;
            bool sameLine = (cfg.quote_author_pos != "below");
            if (sameLine) { qbw = qw + (aw ? gap + aw : 0); qbh = (float)qMax(qh, ah); }
            else { qbw = qMax(qw, aw); qbh = (float)(qh + (aw ? ah * 0.5 : 0)); }
            float qBlockH = qbh + 20.0f;
            float g = 24.0f;
            const std::string& vp = cfg.datetime_vpos;
            if (vp == "top") { dtY = 40.0f; qY = dtY + dtH + g; }
            else if (vp == "middle") { float total = dtH + g + qBlockH; dtY = (height() - total) / 2.0f; qY = dtY + dtH + g; }
            else { qY = height() - 60.0f - qBlockH; dtY = qY - g - dtH; }
        }
    }

    if (cfg.show_datetime) {
        QString dt = QDateTime::currentDateTime().toString(QString::fromUtf8(cfg.datetime_format.c_str()));
        drawLabel(p, dt, (float)cfg.datetime_font_size, cfg.datetime_hpos, cfg.datetime_vpos,
                  argbToColor(cfg.datetime_color), dtY);
    }
    if (cfg.show_quote) drawQuote(p, qY);

    drawButtons(p);
    drawArrows(p);
}

void ScreenWidget::drawLabel(QPainter& p, const QString& text, float size,
                             const std::string& hpos, const std::string& vpos, QColor color, float yOverride) {
    const Config& cfg = ctl_->config();
    QFont f(QString::fromUtf8(cfg.font_name.c_str()));
    f.setPixelSize((int)size);
    QFontMetricsF fm(f);
    qreal tw = fm.horizontalAdvance(text);
    qreal th = size * 1.6;

    qreal x;
    if (hpos == "center") x = (width() - tw) / 2.0;
    else if (hpos == "right") x = width() - 40.0 - tw;
    else x = 40.0;

    qreal y;
    if (yOverride >= 0.0f) y = yOverride;
    else if (vpos == "middle") y = (height() - th) / 2.0;
    else if (vpos == "bottom") y = height() - 40.0 - th;
    else y = 40.0;

    p.setPen(Qt::NoPen);
    p.setBrush(argbToColor(cfg.panel_color));
    p.drawRoundedRect(QRectF(x - 16, y - 8, tw + 32, th + 16), 12, 12);

    p.setFont(f);
    p.setPen(color);
    p.drawText(QRectF(x, y, tw, th), Qt::AlignLeft | Qt::AlignVCenter, text);
}

void ScreenWidget::drawQuote(QPainter& p, float yOverride) {
    const Config& cfg = ctl_->config();
    const Sentence* s = ctl_->currentSentence();
    if (!s) return;

    QString quote = QString::fromUtf8(s->quote.c_str());
    QString author = s->author.empty() ? QString() : (QStringLiteral("—— ") + QString::fromUtf8(s->author.c_str()));

    float qs = (float)cfg.quote_font_size;
    float as_ = (float)cfg.quote_author_font_size;
    qreal qh = qs * 1.6, ah = as_ * 1.5;

    QFont qf(QString::fromUtf8(cfg.font_name.c_str()));
    qf.setPixelSize((int)qs);
    QFontMetricsF qfm(qf);
    qreal qw = qfm.horizontalAdvance(quote);
    qreal aw = author.isEmpty() ? 0 : qfm.horizontalAdvance(author);

    float gap = qs * 0.4f;
    bool sameLine = (cfg.quote_author_pos != "below");
    qreal bw, bh;
    if (sameLine) { bw = qw + (aw ? gap + aw : 0); bh = qMax(qh, ah); }
    else { bw = qMax(qw, aw); bh = qh + (aw ? ah * 0.5 : 0); }

    qreal x;
    if (cfg.quote_hpos == "center") x = (width() - bw) / 2.0;
    else if (cfg.quote_hpos == "right") x = width() - 40.0 - bw;
    else x = 40.0;

    qreal y;
    if (yOverride >= 0.0f) y = yOverride;
    else if (cfg.quote_vpos == "middle") y = (height() - bh) / 2.0;
    else if (cfg.quote_vpos == "bottom") y = height() - 60.0 - bh;
    else y = 60.0;

    p.setPen(Qt::NoPen);
    p.setBrush(argbToColor(cfg.panel_color));
    p.drawRoundedRect(QRectF(x - 18, y - 10, bw + 36, bh + 20), 12, 12);

    QColor c = argbToColor(cfg.quote_color);
    p.setFont(qf);
    p.setPen(c);
    p.drawText(QRectF(x, y, qw, qh), Qt::AlignLeft | Qt::AlignVCenter, quote);

    if (!author.isEmpty()) {
        QFont af(QString::fromUtf8(cfg.font_name.c_str()));
        af.setPixelSize((int)as_);
        p.setFont(af);
        p.setPen(c);
        if (sameLine) {
            qreal ay = y + (qh - ah) / 2.0;
            p.drawText(QRectF(x + qw + gap, ay, aw, ah), Qt::AlignLeft | Qt::AlignVCenter, author);
        } else {
            p.drawText(QRectF(x, y + qh - qs * 0.2, aw, ah), Qt::AlignLeft | Qt::AlignVCenter, author);
        }
    }
}

void ScreenWidget::drawButtons(QPainter& p) {
    QColor panel = argbToColor(ctl_->config().panel_color);
    p.setPen(Qt::NoPen);
    p.setBrush(panel);
    p.drawRoundedRect(QRectF(likeRect_).adjusted(-8, -8, 8, 8), 14, 14);
    p.drawRoundedRect(QRectF(settingsRect_).adjusted(-8, -8, 8, 8), 14, 14);

    bool liked = false;
    if (!ctl_->gallery().empty() && (size_t)ctl_->currentIndex() < ctl_->gallery().size())
        liked = ctl_->gallery().at((size_t)ctl_->currentIndex()).liked;

    const QPixmap& like = liked ? like2_ : like1_;
    if (!like.isNull()) p.drawPixmap(likeRect_, like);
    else drawHeart(p, likeRect_, liked);

    drawGear(p, settingsRect_, panel);
}

void ScreenWidget::drawArrows(QPainter& p) {
    if (!ctl_->arrowsVisible()) return;
    QColor norm(200, 255, 255, 255);
    QColor hot(255, 255, 255, 255);
    drawArrow(p, leftArrow_.center().x(), leftArrow_.center().y(), 26, ctl_->hoverArrow() == -1 ? hot : norm, true);
    drawArrow(p, rightArrow_.center().x(), rightArrow_.center().y(), 26, ctl_->hoverArrow() == 1 ? hot : norm, false);
}

void ScreenWidget::mouseMoveEvent(QMouseEvent* e) {
    ctl_->setArrowsVisible(true);
    QPoint pt = e->pos();
    int hover = 0;
    if (leftArrow_.contains(pt)) hover = -1;
    else if (rightArrow_.contains(pt)) hover = 1;
    ctl_->setHoverArrow(hover);
    QWidget::mouseMoveEvent(e);
}

void ScreenWidget::mousePressEvent(QMouseEvent* e) {
    if (e->button() != Qt::LeftButton) return;
    QPoint pt = e->pos();
    if (likeRect_.contains(pt)) ctl_->toggleLike();
    else if (settingsRect_.contains(pt)) ctl_->requestSettings();
    else if (leftArrow_.contains(pt)) ctl_->previous();
    else if (rightArrow_.contains(pt)) ctl_->next();
    else ctl_->requestExit();
}

void ScreenWidget::keyPressEvent(QKeyEvent*) {
    ctl_->requestExit();
}
