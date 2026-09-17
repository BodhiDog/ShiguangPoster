#include "app.h"
#include "screensaver.h"
#include "settings.h"
#include "idle.h"
#include "util.h"
#include <QApplication>
#include <QSystemTrayIcon>
#include <QMenu>
#include <QAction>
#include <QStyle>
#include <QIcon>

App::App(Config& cfg, ImageGallery& gallery, SentenceStore& sentences, QObject* parent)
    : QObject(parent), cfg_(cfg), gallery_(gallery), sentences_(sentences) {}

void App::setup() {
    tray_ = new QSystemTrayIcon(this);
    QIcon icon(pathJoin(resDir(), "backstage.png"));
    if (icon.isNull()) icon = QApplication::style()->standardIcon(QStyle::SP_ComputerIcon);
    tray_->setIcon(icon);
    tray_->setToolTip(QStringLiteral("屏幕海报"));

    auto* menu = new QMenu();
    QAction* showAction = menu->addAction(QStringLiteral("显示屏保"));
    QAction* settingsAction = menu->addAction(QStringLiteral("设置"));
    menu->addSeparator();
    QAction* quitAction = menu->addAction(QStringLiteral("退出"));
    connect(showAction, &QAction::triggered, this, &App::startSaver);
    connect(settingsAction, &QAction::triggered, this, &App::openSettings);
    connect(quitAction, &QAction::triggered, this, &App::quit);
    tray_->setContextMenu(menu);

    connect(tray_, &QSystemTrayIcon::activated, this, &App::onTrayActivated);
    tray_->show();

    idleTimer_.setInterval(1000);
    connect(&idleTimer_, &QTimer::timeout, this, &App::checkIdle);
    idleTimer_.start();
}

void App::onTrayActivated(QSystemTrayIcon::ActivationReason reason) {
    if (reason == QSystemTrayIcon::Trigger) startSaver();
    else if (reason == QSystemTrayIcon::DoubleClick) openSettings();
}

void App::checkIdle() {
    if (saver_) return;
    if (settings_ && settings_->isVisible()) return;
    if (systemIdleMs() >= (quint64)cfg_.idle_timeout_seconds * 1000) startSaver();
}

void App::startSaver() {
    if (saver_) return;
    saver_ = new ScreensaverController(cfg_, gallery_, sentences_, this);
    connect(saver_, &ScreensaverController::exitRequested, this, &App::stopSaver);
    connect(saver_, &ScreensaverController::settingsRequested, this, &App::openSettings);
    saver_->start();
}

void App::stopSaver() {
    if (saver_) {
        saver_->stop();
        saver_->deleteLater();
        saver_ = nullptr;
    }
}

void App::openSettings() {
    stopSaver();
    if (settings_) {
        settings_->raise();
        settings_->activateWindow();
        return;
    }
    settings_ = new Settings(cfg_, gallery_, sentences_, nullptr);
    settings_->setAttribute(Qt::WA_DeleteOnClose);
    connect(settings_, &QObject::destroyed, this, [this] { settings_ = nullptr; });
    settings_->show();
}

void App::quit() {
    QApplication::quit();
}
