#pragma once
#include <QString>
#include <string>
#include <vector>

struct RemoteImage {
    std::string id;
    std::string url;
    std::string title;
};

// Reserved interface for future cloud image sources (Bing/Unsplash/custom API...).
class ICloudImageProvider {
public:
    virtual ~ICloudImageProvider() = default;
    virtual bool authenticate() = 0;
    virtual std::vector<RemoteImage> fetchImages(int count) = 0;
    virtual bool downloadImage(const RemoteImage& img, const QString& dest_path) = 0;
};

class ImageGallery;
class SentenceStore;

// Reserved entry point for "获取云端图片 / 更新图库" features.
class GalleryUpdater {
public:
    void setProvider(ICloudImageProvider* provider) { provider_ = provider; }
    void update(ImageGallery& gallery, SentenceStore& sentences);

private:
    ICloudImageProvider* provider_ = nullptr;
};
