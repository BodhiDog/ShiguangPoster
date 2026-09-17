#include <QApplication>
#include <QFont>
#include "config.h"
#include "gallery.h"
#include "sentences.h"
#include "app.h"

int main(int argc, char** argv) {
#if QT_VERSION < QT_VERSION_CHECK(6, 0, 0)
    QApplication::setAttribute(Qt::AA_EnableHighDpiScaling);
    QApplication::setAttribute(Qt::AA_UseHighDpiPixmaps);
#endif
    QApplication app(argc, argv);
    QApplication::setApplicationName(QStringLiteral("ScreenPoster"));
    QApplication::setQuitOnLastWindowClosed(false);

    QFont f(QStringLiteral("Microsoft YaHei"));
    QApplication::setFont(f);

    Config cfg;
    cfg.load();
    ImageGallery gallery;
    gallery.scan();
    SentenceStore sentences;
    sentences.load();

    App a(cfg, gallery, sentences);
    a.setup();
    return app.exec();
}
