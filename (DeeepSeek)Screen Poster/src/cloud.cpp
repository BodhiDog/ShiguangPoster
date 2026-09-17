#include "cloud.h"
#include "gallery.h"
#include "sentences.h"
#include "util.h"
#include <set>

void GalleryUpdater::update(ImageGallery& gallery, SentenceStore& sentences) {
    if (provider_) {
        std::vector<RemoteImage> remote = provider_->fetchImages(10);
        for (auto& img : remote) {
            provider_->downloadImage(img, pathJoin(picsDir(), QString::fromUtf8(img.id.c_str()) + ".jpg"));
        }
    }
    std::set<std::string> before;
    for (auto& n : gallery.imageNames()) before.insert(n);
    gallery.scan();
    for (auto& name : gallery.imageNames()) {
        if (!before.count(name)) {
            sentences.addImage(name);
        }
    }
}
