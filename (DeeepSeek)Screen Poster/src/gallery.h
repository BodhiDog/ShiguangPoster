#pragma once
#include <QString>
#include <string>
#include <vector>

struct ImageInfo {
    QString path;
    std::string name;   // utf-8 filename
    bool liked = false;
    double weight = 1.0;
};

class ImageGallery {
public:
    void scan();
    void loadLikes();
    void saveLikes() const;
    void setLiked(size_t idx, bool liked);
    bool toggleLike(size_t idx);
    size_t pickRandom() const;
    int indexOf(const std::string& name) const;

    const std::vector<ImageInfo>& images() const { return images_; }
    const ImageInfo& at(size_t i) const { return images_[i]; }
    size_t size() const { return images_.size(); }
    bool empty() const { return images_.empty(); }
    std::vector<std::string> imageNames() const;

private:
    std::vector<ImageInfo> images_;
    static constexpr double LIKE_BOOST = 3.0;
};
