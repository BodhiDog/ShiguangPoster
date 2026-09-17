#include "util.h"
#include <QCoreApplication>
#include <QDir>
#include <QFile>
#include <QFileInfo>
#include <algorithm>

QString appDir() {
    return QCoreApplication::applicationDirPath();
}

QString pathJoin(const QString& a, const QString& b) {
    if (a.isEmpty()) return b;
    if (b.isEmpty()) return a;
    if (a.endsWith('/') || a.endsWith('\\')) return a + b;
    return a + '/' + b;
}

QString picsDir() { return pathJoin(appDir(), "pictures"); }
QString confDir() { return pathJoin(appDir(), "config"); }
QString resDir() { return pathJoin(appDir(), "resources"); }
QString sentencesPath() { return pathJoin(appDir(), "sentences.csv"); }

void ensureDir(const QString& dir) {
    if (!dir.isEmpty()) QDir().mkpath(dir);
}

bool fileExists(const QString& path) {
    return QFileInfo::exists(path);
}

QByteArray readFile(const QString& path) {
    QFile f(path);
    if (!f.open(QIODevice::ReadOnly)) return QByteArray();
    return f.readAll();
}

bool writeFile(const QString& path, const QByteArray& data) {
    QFile f(path);
    if (!f.open(QIODevice::WriteOnly | QIODevice::Truncate)) return false;
    return f.write(data) == data.size();
}

QColor argbToColor(unsigned int v) {
    return QColor((v >> 16) & 0xff, (v >> 8) & 0xff, v & 0xff, (v >> 24) & 0xff);
}

std::string trim(const std::string& s) {
    size_t a = s.find_first_not_of(" \t\r\n");
    if (a == std::string::npos) return "";
    size_t b = s.find_last_not_of(" \t\r\n");
    return s.substr(a, b - a + 1);
}

std::string toLower(const std::string& s) {
    std::string r = s;
    std::transform(r.begin(), r.end(), r.begin(), [](unsigned char c) { return (char)::tolower(c); });
    return r;
}

int parseInt(const std::string& s, int def) {
    if (trim(s).empty()) return def;
    try { return std::stoi(trim(s)); } catch (...) { return def; }
}

std::vector<std::string> split(const std::string& s, char delim) {
    std::vector<std::string> out;
    std::string cur;
    for (char c : s) {
        if (c == delim) { out.push_back(cur); cur.clear(); }
        else cur += c;
    }
    out.push_back(cur);
    return out;
}
