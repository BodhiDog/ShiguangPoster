#include "gallery.h"
#include "util.h"
#include <QDir>
#include <QFileInfo>
#include <QStringList>
#include <algorithm>
#include <random>
#include <set>

static bool isImageExt(const QString& lower) {
    static const QStringList exts = {".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp"};
    return exts.contains(lower);
}

void ImageGallery::scan() {
    images_.clear();
    QString dir = picsDir();
    ensureDir(dir);
    QDir d(dir);
    const QFileInfoList entries = d.entryInfoList(QDir::Files, QDir::Name);
    for (const QFileInfo& fi : entries) {
        QString suffix = fi.suffix().toLower();
        if (!isImageExt("." + suffix)) continue;
        ImageInfo info;
        info.path = fi.absoluteFilePath();
        info.name = fi.fileName().toUtf8().toStdString();
        images_.push_back(info);
    }
    loadLikes();
}

void ImageGallery::loadLikes() {
    QString path = pathJoin(confDir(), "likes.txt");
    QByteArray data = readFile(path);
    std::set<std::string> liked;
    std::string text(data.constData(), (size_t)data.size());
    for (auto& line : split(text, '\n')) {
        std::string t = trim(line);
        if (!t.empty()) liked.insert(t);
    }
    for (auto& im : images_) {
        im.liked = liked.count(im.name) > 0;
        im.weight = 1.0 + (im.liked ? LIKE_BOOST : 0.0);
    }
}

void ImageGallery::saveLikes() const {
    ensureDir(confDir());
    std::string out;
    for (auto& im : images_) if (im.liked) out += im.name + "\n";
    writeFile(pathJoin(confDir(), "likes.txt"), QByteArray(out.c_str(), (int)out.size()));
}

void ImageGallery::setLiked(size_t idx, bool liked) {
    if (idx >= images_.size()) return;
    images_[idx].liked = liked;
    images_[idx].weight = 1.0 + (liked ? LIKE_BOOST : 0.0);
    saveLikes();
}

bool ImageGallery::toggleLike(size_t idx) {
    if (idx >= images_.size()) return false;
    setLiked(idx, !images_[idx].liked);
    return images_[idx].liked;
}

size_t ImageGallery::pickRandom() const {
    if (images_.empty()) return 0;
    double total = 0.0;
    for (auto& im : images_) total += im.weight;
    static thread_local std::mt19937 rng(std::random_device{}());
    std::uniform_real_distribution<double> dist(0.0, total);
    double r = dist(rng);
    for (size_t i = 0; i < images_.size(); ++i) {
        r -= images_[i].weight;
        if (r <= 0.0) return i;
    }
    return images_.size() - 1;
}

int ImageGallery::indexOf(const std::string& name) const {
    for (size_t i = 0; i < images_.size(); ++i) if (images_[i].name == name) return (int)i;
    return -1;
}

std::vector<std::string> ImageGallery::imageNames() const {
    std::vector<std::string> out;
    for (auto& im : images_) out.push_back(im.name);
    return out;
}
