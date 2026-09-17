#pragma once
#include <QObject>
#include <QTimer>
#include <QSystemTrayIcon>
#include "config.h"
#include "gallery.h"
#include "sentences.h"

class QMenu;
class ScreensaverController;
class Settings;

class App : public QObject {
    Q_OBJECT
public:
    App(Config& cfg, ImageGallery& gallery, SentenceStore& sentences, QObject* parent = nullptr);
    void setup();

private slots:
    void onTrayActivated(QSystemTrayIcon::ActivationReason reason);
    void checkIdle();
    void startSaver();
    void stopSaver();
    void openSettings();
    void quit();

private:
    Config& cfg_;
    ImageGallery& gallery_;
    SentenceStore& sentences_;
    QSystemTrayIcon* tray_ = nullptr;
    QTimer idleTimer_;
    ScreensaverController* saver_ = nullptr;
    Settings* settings_ = nullptr;
};
