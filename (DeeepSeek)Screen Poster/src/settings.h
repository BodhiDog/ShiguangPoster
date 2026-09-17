#pragma once
#include <QDialog>
#include "config.h"
#include "gallery.h"
#include "sentences.h"

class QSpinBox;
class QComboBox;
class QCheckBox;
class QLineEdit;
class QListWidget;

class Settings : public QDialog {
    Q_OBJECT
public:
    Settings(Config& cfg, ImageGallery& gallery, SentenceStore& sentences, QWidget* parent = nullptr);

private slots:
    void onSave();
    void onAddQuote();
    void onRemoveQuote();
    void onScanImages();
    void onOpenDir();

private:
    void refreshLists();

    Config& cfg_;
    ImageGallery& gallery_;
    SentenceStore& sentences_;

    QSpinBox* idleSpin_ = nullptr;
    QSpinBox* slideSpin_ = nullptr;
    QComboBox* transitionCombo_ = nullptr;
    QCheckBox* showDatetime_ = nullptr;
    QCheckBox* showQuote_ = nullptr;
    QLineEdit* dtFormatEdit_ = nullptr;
    QComboBox* dtHpos_ = nullptr;
    QComboBox* dtVpos_ = nullptr;
    QSpinBox* dtFontSpin_ = nullptr;
    QComboBox* qHpos_ = nullptr;
    QComboBox* qVpos_ = nullptr;
    QComboBox* authorPos_ = nullptr;
    QSpinBox* qFontSpin_ = nullptr;
    QListWidget* quoteList_ = nullptr;
    QListWidget* imageList_ = nullptr;
};
