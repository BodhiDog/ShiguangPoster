#pragma once
#include <string>
#include <vector>
#include <map>
#include "matcher.h"

struct Sentence {
    std::string quote;
    std::string author;
    std::map<std::string, double> scores; // image name -> match score 0..1
};

class SentenceStore {
public:
    bool load();
    bool save() const;

    const std::vector<Sentence>& all() const { return sentences_; }
    size_t size() const { return sentences_.size(); }
    const Sentence& at(size_t i) const { return sentences_[i]; }

    int addQuote(const std::string& quote, const std::string& author,
                 const std::vector<std::string>& image_names);
    bool removeQuote(size_t idx);

    void addImage(const std::string& image_name);

    int pickForImage(const std::string& image_name) const;
    int pickRandom() const;

private:
    std::vector<Sentence> sentences_;
    Matcher matcher_;
};
