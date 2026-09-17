#pragma once
#include <QString>
#include <string>

struct Tone {
    double bright = 0.5;
    double warm = 0.5;
};

struct Mood {
    double bright = 0.5;
    double warm = 0.5;
};

class Matcher {
public:
    Tone analyzeImage(const QString& path);
    Mood analyzeQuote(const std::string& quote);
    double score(const Tone& t, const Mood& m) const;
};
