#pragma once
#include <QString>
#include <QByteArray>
#include <QColor>
#include <string>
#include <vector>

QString appDir();
QString picsDir();
QString confDir();
QString resDir();
QString sentencesPath();
QString pathJoin(const QString& a, const QString& b);
void ensureDir(const QString& dir);
bool fileExists(const QString& path);
QByteArray readFile(const QString& path);
bool writeFile(const QString& path, const QByteArray& data);
QColor argbToColor(unsigned int v);

std::string trim(const std::string& s);
std::string toLower(const std::string& s);
int parseInt(const std::string& s, int def);
std::vector<std::string> split(const std::string& s, char delim);
