#include "settings.h"
#include "util.h"
#include "cloud.h"
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QFormLayout>
#include <QGroupBox>
#include <QSpinBox>
#include <QComboBox>
#include <QCheckBox>
#include <QLineEdit>
#include <QListWidget>
#include <QPushButton>
#include <QDialogButtonBox>
#include <QLabel>
#include <QDesktopServices>
#include <QUrl>
#include <QStringList>

namespace {
const QStringList kHposValues = {"left", "center", "right"};
const QStringList kHposLabels = {QStringLiteral("靠左"), QStringLiteral("居中"), QStringLiteral("靠右")};
const QStringList kVposValues = {"top", "middle", "bottom"};
const QStringList kVposLabels = {QStringLiteral("顶部"), QStringLiteral("中部"), QStringLiteral("底部")};
const QStringList kTransValues = {"fade", "blinds", "nonlinear", "none"};
const QStringList kTransLabels = {QStringLiteral("渐变切换"), QStringLiteral("百叶窗切换"),
                                  QStringLiteral("非线性动画切换"), QStringLiteral("无动画")};
const QStringList kAuthorValues = {"right", "below"};
const QStringList kAuthorLabels = {QStringLiteral("右侧"), QStringLiteral("下方")};

int comboIndexOf(const QStringList& values, const QString& v) {
    int i = values.indexOf(v);
    return i < 0 ? 0 : i;
}

QString comboValue(QComboBox* cb, const QStringList& values) {
    int sel = cb->currentIndex();
    if (sel < 0 || sel >= values.size()) return values.isEmpty() ? QString() : values[0];
    return values[sel];
}

QComboBox* makeCombo(const QStringList& labels) {
    auto* cb = new QComboBox();
    cb->addItems(labels);
    return cb;
}
}

Settings::Settings(Config& cfg, ImageGallery& gallery, SentenceStore& sentences, QWidget* parent)
    : QDialog(parent), cfg_(cfg), gallery_(gallery), sentences_(sentences) {
    setWindowTitle(QStringLiteral("屏幕海报 - 设置"));
    setModal(false);

    auto* root = new QVBoxLayout(this);

    auto* general = new QGroupBox(QStringLiteral("常规"));
    auto* gf = new QFormLayout(general);
    idleSpin_ = new QSpinBox(); idleSpin_->setRange(5, 86400); idleSpin_->setSuffix(QStringLiteral(" 秒"));
    slideSpin_ = new QSpinBox(); slideSpin_->setRange(1, 3600); slideSpin_->setSuffix(QStringLiteral(" 秒"));
    transitionCombo_ = makeCombo(kTransLabels);
    showDatetime_ = new QCheckBox(QStringLiteral("显示日期时间"));
    showQuote_ = new QCheckBox(QStringLiteral("显示名言"));
    gf->addRow(QStringLiteral("无操作进入屏保:"), idleSpin_);
    gf->addRow(QStringLiteral("幻灯片切换间隔:"), slideSpin_);
    gf->addRow(QStringLiteral("切换动画:"), transitionCombo_);
    gf->addRow(showDatetime_);
    gf->addRow(showQuote_);

    auto* dtGroup = new QGroupBox(QStringLiteral("日期时间"));
    auto* dtf = new QFormLayout(dtGroup);
    dtFormatEdit_ = new QLineEdit();
    dtHpos_ = makeCombo(kHposLabels);
    dtVpos_ = makeCombo(kVposLabels);
    dtFontSpin_ = new QSpinBox(); dtFontSpin_->setRange(8, 200);
    dtf->addRow(QStringLiteral("格式:"), dtFormatEdit_);
    dtf->addRow(QStringLiteral("水平位置:"), dtHpos_);
    dtf->addRow(QStringLiteral("垂直位置:"), dtVpos_);
    dtf->addRow(QStringLiteral("字号:"), dtFontSpin_);

    auto* qGroup = new QGroupBox(QStringLiteral("名言"));
    auto* qf = new QFormLayout(qGroup);
    qHpos_ = makeCombo(kHposLabels);
    qVpos_ = makeCombo(kVposLabels);
    authorPos_ = makeCombo(kAuthorLabels);
    qFontSpin_ = new QSpinBox(); qFontSpin_->setRange(8, 200);
    qf->addRow(QStringLiteral("水平位置:"), qHpos_);
    qf->addRow(QStringLiteral("垂直位置:"), qVpos_);
    qf->addRow(QStringLiteral("作者位置:"), authorPos_);
    qf->addRow(QStringLiteral("字号:"), qFontSpin_);

    auto* qListGroup = new QGroupBox(QStringLiteral("名言管理"));
    auto* ql = new QVBoxLayout(qListGroup);
    quoteList_ = new QListWidget();
    auto* qbtn = new QHBoxLayout();
    auto* addBtn = new QPushButton(QStringLiteral("添加名言"));
    auto* delBtn = new QPushButton(QStringLiteral("删除名言"));
    qbtn->addWidget(addBtn); qbtn->addWidget(delBtn); qbtn->addStretch();
    ql->addWidget(quoteList_); ql->addLayout(qbtn);

    auto* imgGroup = new QGroupBox(QStringLiteral("图片管理"));
    auto* il = new QVBoxLayout(imgGroup);
    imageList_ = new QListWidget();
    auto* ibtn = new QHBoxLayout();
    auto* scanBtn = new QPushButton(QStringLiteral("扫描新图片"));
    auto* openBtn = new QPushButton(QStringLiteral("打开图片文件夹"));
    ibtn->addWidget(scanBtn); ibtn->addWidget(openBtn); ibtn->addStretch();
    il->addWidget(imageList_); il->addLayout(ibtn);

    auto* buttons = new QDialogButtonBox();
    auto* saveBtn = buttons->addButton(QStringLiteral("保存"), QDialogButtonBox::AcceptRole);
    auto* cancelBtn = buttons->addButton(QStringLiteral("取消"), QDialogButtonBox::RejectRole);

    root->addWidget(general);
    root->addWidget(dtGroup);
    root->addWidget(qGroup);
    root->addWidget(qListGroup);
    root->addWidget(imgGroup);
    root->addWidget(buttons);

    connect(saveBtn, &QPushButton::clicked, this, &Settings::onSave);
    connect(cancelBtn, &QPushButton::clicked, this, &QDialog::reject);
    connect(addBtn, &QPushButton::clicked, this, &Settings::onAddQuote);
    connect(delBtn, &QPushButton::clicked, this, &Settings::onRemoveQuote);
    connect(scanBtn, &QPushButton::clicked, this, &Settings::onScanImages);
    connect(openBtn, &QPushButton::clicked, this, &Settings::onOpenDir);

    idleSpin_->setValue(cfg_.idle_timeout_seconds);
    slideSpin_->setValue(cfg_.slide_interval_seconds);
    transitionCombo_->setCurrentIndex(comboIndexOf(kTransValues, QString::fromUtf8(cfg_.transition_effect.c_str())));
    showDatetime_->setChecked(cfg_.show_datetime);
    showQuote_->setChecked(cfg_.show_quote);

    dtFormatEdit_->setText(QString::fromUtf8(cfg_.datetime_format.c_str()));
    dtHpos_->setCurrentIndex(comboIndexOf(kHposValues, QString::fromUtf8(cfg_.datetime_hpos.c_str())));
    dtVpos_->setCurrentIndex(comboIndexOf(kVposValues, QString::fromUtf8(cfg_.datetime_vpos.c_str())));
    dtFontSpin_->setValue(cfg_.datetime_font_size);

    qHpos_->setCurrentIndex(comboIndexOf(kHposValues, QString::fromUtf8(cfg_.quote_hpos.c_str())));
    qVpos_->setCurrentIndex(comboIndexOf(kVposValues, QString::fromUtf8(cfg_.quote_vpos.c_str())));
    authorPos_->setCurrentIndex(comboIndexOf(kAuthorValues, QString::fromUtf8(cfg_.quote_author_pos.c_str())));
    qFontSpin_->setValue(cfg_.quote_font_size);

    refreshLists();
}

void Settings::refreshLists() {
    quoteList_->clear();
    for (auto& s : sentences_.all()) {
        QString line = QString::fromUtf8(s.quote.c_str());
        if (!s.author.empty()) line += QStringLiteral("  —— ") + QString::fromUtf8(s.author.c_str());
        quoteList_->addItem(line);
    }
    imageList_->clear();
    for (auto& im : gallery_.images()) {
        QString line = QString::fromUtf8(im.name.c_str());
        if (im.liked) line += QStringLiteral("  ♥");
        imageList_->addItem(line);
    }
}

void Settings::onSave() {
    cfg_.idle_timeout_seconds = idleSpin_->value();
    cfg_.slide_interval_seconds = slideSpin_->value();
    cfg_.transition_effect = comboValue(transitionCombo_, kTransValues).toUtf8().toStdString();
    cfg_.show_datetime = showDatetime_->isChecked();
    cfg_.show_quote = showQuote_->isChecked();

    cfg_.datetime_format = dtFormatEdit_->text().toUtf8().toStdString();
    cfg_.datetime_hpos = comboValue(dtHpos_, kHposValues).toUtf8().toStdString();
    cfg_.datetime_vpos = comboValue(dtVpos_, kVposValues).toUtf8().toStdString();
    cfg_.datetime_font_size = dtFontSpin_->value();

    cfg_.quote_hpos = comboValue(qHpos_, kHposValues).toUtf8().toStdString();
    cfg_.quote_vpos = comboValue(qVpos_, kVposValues).toUtf8().toStdString();
    cfg_.quote_author_pos = comboValue(authorPos_, kAuthorValues).toUtf8().toStdString();
    cfg_.quote_font_size = qFontSpin_->value();

    cfg_.save();
    gallery_.saveLikes();
    accept();
}

void Settings::onAddQuote() {
    QDialog dlg(this);
    dlg.setWindowTitle(QStringLiteral("添加名言"));
    auto* form = new QFormLayout(&dlg);
    auto* quoteEdit = new QLineEdit();
    auto* authorEdit = new QLineEdit();
    form->addRow(QStringLiteral("名言内容:"), quoteEdit);
    form->addRow(QStringLiteral("作者:"), authorEdit);
    auto* bb = new QDialogButtonBox(QDialogButtonBox::Ok | QDialogButtonBox::Cancel);
    form->addRow(bb);
    connect(bb, &QDialogButtonBox::accepted, &dlg, &QDialog::accept);
    connect(bb, &QDialogButtonBox::rejected, &dlg, &QDialog::reject);
    if (dlg.exec() == QDialog::Accepted) {
        std::string quote = quoteEdit->text().toUtf8().toStdString();
        std::string author = authorEdit->text().toUtf8().toStdString();
        if (!quote.empty()) {
            sentences_.addQuote(quote, author, gallery_.imageNames());
            refreshLists();
        }
    }
}

void Settings::onRemoveQuote() {
    int row = quoteList_->currentRow();
    if (row >= 0) {
        sentences_.removeQuote((size_t)row);
        refreshLists();
    }
}

void Settings::onScanImages() {
    GalleryUpdater upd;
    upd.update(gallery_, sentences_);
    refreshLists();
}

void Settings::onOpenDir() {
    ensureDir(picsDir());
    QDesktopServices::openUrl(QUrl::fromLocalFile(picsDir()));
}
