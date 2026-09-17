#pragma once
#include <QObject>
#include <QWidget>
#include <QImage>
#include <QPixmap>
#include <QTimer>
#include <QPointer>
#include <QList>
#include <QRect>
#include <QColor>
#include "config.h"
#include "gallery.h"
#include "sentences.h"

class QScreen;
class QPainter;
class QPaintEvent;
class QMouseEvent;
class QKeyEvent;
class QResizeEvent;
class ScreensaverController;

class ScreenWidget : public QWidget {
    Q_OBJECT
public:
    ScreenWidget(ScreensaverController* ctl, QScreen* screen);

protected:
    void paintEvent(QPaintEvent*) override;
    void mouseMoveEvent(QMouseEvent*) override;
    void mousePressEvent(QMouseEvent*) override;
    void keyPressEvent(QKeyEvent*) override;
    void resizeEvent(QResizeEvent*) override;

private:
    void updateRects();
    void drawLabel(QPainter& p, const QString& text, float size,
                   const std::string& hpos, const std::string& vpos, const QColor& color, float yOverride);
    void drawQuote(QPainter& p, float yOverride);
    void drawButtons(QPainter& p);
    void drawArrows(QPainter& p);

    ScreensaverController* ctl_;
    QRect likeRect_, settingsRect_, leftArrow_, rightArrow_;
    QPixmap like1_, like2_;
};

class ScreensaverController : public QObject {
    Q_OBJECT
public:
    ScreensaverController(Config& cfg, ImageGallery& gallery, SentenceStore& sentences);
    ~ScreensaverController() override;

    void start();
    void stop();

    const Config& config() const { return cfg_; }
    const ImageGallery& gallery() const { return gallery_; }
    int currentIndex() const { return curIndex_; }
    const QImage& currentImage() const { return curImage_; }
    const QImage& nextImage() const { return nextImage_; }
    bool transitioning() const { return transitioning_; }
    float progress() const { return progress_; }
    bool arrowsVisible() const { return arrowsVisible_; }
    int hoverArrow() const { return hoverArrow_; }
    const Sentence* currentSentence() const;

    void toggleLike();
    void previous();
    void next();
    void requestExit() { emit exitRequested(); }
    void requestSettings() { emit settingsRequested(); }
    void setHoverArrow(int h);
    void setArrowsVisible(bool v);

signals:
    void exitRequested();
    void settingsRequested();
    void stateChanged();

private slots:
    void onSlide();
    void onFrame();
    void onClock();

private:
    void loadCurrent();
    void pickQuote();
    void switchTo(int idx);
    void finishTransition();

    Config& cfg_;
    ImageGallery& gallery_;
    SentenceStore& sentences_;
    int curIndex_ = 0;
    int nextIndex_ = 0;
    QImage curImage_, nextImage_;
    bool transitioning_ = false;
    qint64 transStart_ = 0;
    float progress_ = 0.0f;
    int quoteIndex_ = -1;
    bool arrowsVisible_ = false;
    int hoverArrow_ = 0;
    QTimer slideTimer_, frameTimer_, clockTimer_;
    QList<QPointer<ScreenWidget>> screens_;
};
