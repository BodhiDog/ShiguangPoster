#include "sentences.h"
#include "csv.h"
#include "util.h"
#include <random>
#include <cmath>
#include <cstdio>

bool SentenceStore::load() {
    sentences_.clear();
    QByteArray data = readFile(sentencesPath());
    if (data.isEmpty()) return false;
    std::string text(data.constData(), (size_t)data.size());
    bool first = true;
    for (auto& line : split(text, '\n')) {
        if (trim(line).empty()) continue;
        auto f = parse_csv_line(line);
        if (first) { first = false; continue; } // header
        if (f.size() < 2) continue;
        Sentence s;
        s.quote = f[0];
        s.author = f[1];
        if (f.size() >= 3 && !f[2].empty()) {
            for (auto& pair : split(f[2], ';')) {
                auto p = trim(pair);
                size_t eq = p.find('=');
                if (eq == std::string::npos) continue;
                std::string img = trim(p.substr(0, eq));
                std::string sc = trim(p.substr(eq + 1));
                try { s.scores[img] = std::stod(sc); } catch (...) {}
            }
        }
        sentences_.push_back(s);
    }
    return true;
}

bool SentenceStore::save() const {
    std::string out = "quote,author,image_scores\n";
    for (auto& s : sentences_) {
        std::vector<std::string> f;
        f.push_back(s.quote);
        f.push_back(s.author);
        std::string scores;
        for (auto& kv : s.scores) {
            char buf[64];
            snprintf(buf, sizeof(buf), "%s=%.3f;", kv.first.c_str(), kv.second);
            scores += buf;
        }
        f.push_back(scores);
        out += csv_join(f) + "\n";
    }
    return writeFile(sentencesPath(), QByteArray(out.c_str(), (int)out.size()));
}

int SentenceStore::addQuote(const std::string& quote, const std::string& author,
                            const std::vector<std::string>& image_names) {
    Sentence s;
    s.quote = quote;
    s.author = author;
    Mood mood = matcher_.analyzeQuote(quote);
    for (auto& name : image_names) {
        Tone t = matcher_.analyzeImage(pathJoin(picsDir(), QString::fromUtf8(name.c_str())));
        s.scores[name] = matcher_.score(t, mood);
    }
    sentences_.push_back(s);
    save();
    return (int)sentences_.size() - 1;
}

bool SentenceStore::removeQuote(size_t idx) {
    if (idx >= sentences_.size()) return false;
    sentences_.erase(sentences_.begin() + idx);
    save();
    return true;
}

void SentenceStore::addImage(const std::string& image_name) {
    Tone t = matcher_.analyzeImage(pathJoin(picsDir(), QString::fromUtf8(image_name.c_str())));
    for (auto& s : sentences_) {
        Mood m = matcher_.analyzeQuote(s.quote);
        s.scores[image_name] = matcher_.score(t, m);
    }
    save();
}

int SentenceStore::pickForImage(const std::string& image_name) const {
    if (sentences_.empty()) return -1;
    static thread_local std::mt19937 rng(std::random_device{}());
    double total = 0.0;
    for (auto& s : sentences_) {
        double sc = 0.1;
        auto it = s.scores.find(image_name);
        if (it != s.scores.end()) sc = 0.1 + it->second * it->second * 4.0;
        total += sc;
    }
    std::uniform_real_distribution<double> dist(0.0, total);
    double r = dist(rng);
    for (size_t i = 0; i < sentences_.size(); ++i) {
        double sc = 0.1;
        auto it = sentences_[i].scores.find(image_name);
        if (it != sentences_[i].scores.end()) sc = 0.1 + it->second * it->second * 4.0;
        r -= sc;
        if (r <= 0.0) return (int)i;
    }
    return (int)sentences_.size() - 1;
}

int SentenceStore::pickRandom() const {
    if (sentences_.empty()) return -1;
    static thread_local std::mt19937 rng(std::random_device{}());
    std::uniform_int_distribution<int> dist(0, (int)sentences_.size() - 1);
    return dist(rng);
}
