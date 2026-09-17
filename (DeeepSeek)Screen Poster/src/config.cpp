#include "config.h"
#include "util.h"
#include <sstream>
#include <cstdio>

namespace {
std::map<std::string, std::map<std::string, std::string>> parse_ini(const std::string& text) {
    std::map<std::string, std::map<std::string, std::string>> ini;
    std::string section;
    std::istringstream ss(text);
    std::string line;
    while (std::getline(ss, line)) {
        std::string t = trim(line);
        if (t.empty() || t[0] == '#' || t[0] == ';') continue;
        if (t.front() == '[' && t.back() == ']') { section = t.substr(1, t.size() - 2); continue; }
        size_t eq = t.find('=');
        if (eq == std::string::npos) continue;
        std::string k = trim(t.substr(0, eq));
        std::string v = trim(t.substr(eq + 1));
        ini[section][k] = v;
    }
    return ini;
}

std::string get_val(const std::map<std::string, std::map<std::string, std::string>>& ini,
                    const std::string& sec, const std::string& key, const std::string& def) {
    auto s = ini.find(sec);
    if (s == ini.end()) return def;
    auto k = s->second.find(key);
    return k == s->second.end() ? def : k->second;
}

unsigned int parse_color(const std::string& s, unsigned int def) {
    std::string t = trim(s);
    if (t.empty()) return def;
    try {
        unsigned long v = std::stoul(t, nullptr, 16);
        if (t.size() <= 6) v |= 0xFF000000ul;
        return (unsigned int)v;
    } catch (...) { return def; }
}
}

bool Config::load() {
    QString path = pathJoin(confDir(), "config.ini");
    QByteArray data = readFile(path);
    if (data.isEmpty()) return false;
    auto ini = parse_ini(std::string(data.constData(), (size_t)data.size()));

    idle_timeout_seconds = parseInt(get_val(ini, "general", "idle_timeout_seconds", "300"), 300);
    slide_interval_seconds = parseInt(get_val(ini, "general", "slide_interval_seconds", "20"), 20);
    transition_effect = get_val(ini, "general", "transition_effect", "fade");
    transition_duration_ms = parseInt(get_val(ini, "general", "transition_duration_ms", "800"), 800);
    show_datetime = get_val(ini, "general", "show_datetime", "1") != "0";
    show_quote = get_val(ini, "general", "show_quote", "1") != "0";

    datetime_format = get_val(ini, "datetime", "format", datetime_format);
    datetime_hpos = get_val(ini, "datetime", "hpos", "left");
    datetime_vpos = get_val(ini, "datetime", "vpos", "top");
    datetime_font_size = parseInt(get_val(ini, "datetime", "font_size", "56"), 56);

    quote_hpos = get_val(ini, "quote", "hpos", "left");
    quote_vpos = get_val(ini, "quote", "vpos", "bottom");
    quote_font_size = parseInt(get_val(ini, "quote", "font_size", "34"), 34);
    quote_author_pos = get_val(ini, "quote", "author_pos", "right");
    quote_author_font_size = parseInt(get_val(ini, "quote", "author_font_size", "20"), 20);
    font_name = get_val(ini, "font", "name", "Microsoft YaHei");

    datetime_color = parse_color(get_val(ini, "colors", "datetime", "FFFFFF"), 0xFFFFFFFF);
    quote_color = parse_color(get_val(ini, "colors", "quote", "FFFFFF"), 0xFFFFFFFF);
    panel_color = parse_color(get_val(ini, "colors", "panel", "80000000"), 0x80000000);

    if (idle_timeout_seconds < 5) idle_timeout_seconds = 5;
    if (slide_interval_seconds < 1) slide_interval_seconds = 1;
    return true;
}

bool Config::save() const {
    QString path = pathJoin(confDir(), "config.ini");
    std::ostringstream os;
    os << "[general]\n";
    os << "idle_timeout_seconds = " << idle_timeout_seconds << "\n";
    os << "slide_interval_seconds = " << slide_interval_seconds << "\n";
    os << "transition_effect = " << transition_effect << "\n";
    os << "transition_duration_ms = " << transition_duration_ms << "\n";
    os << "show_datetime = " << (show_datetime ? 1 : 0) << "\n";
    os << "show_quote = " << (show_quote ? 1 : 0) << "\n\n";
    os << "[datetime]\n";
    os << "format = " << datetime_format << "\n";
    os << "hpos = " << datetime_hpos << "\n";
    os << "vpos = " << datetime_vpos << "\n";
    os << "font_size = " << datetime_font_size << "\n\n";
    os << "[quote]\n";
    os << "hpos = " << quote_hpos << "\n";
    os << "vpos = " << quote_vpos << "\n";
    os << "font_size = " << quote_font_size << "\n";
    os << "author_pos = " << quote_author_pos << "\n";
    os << "author_font_size = " << quote_author_font_size << "\n\n";
    os << "[font]\n";
    os << "name = " << font_name << "\n\n";
    os << "[colors]\n";
    char buf[16];
    snprintf(buf, sizeof(buf), "%06X", datetime_color & 0xFFFFFF);
    os << "datetime = " << buf << "\n";
    snprintf(buf, sizeof(buf), "%06X", quote_color & 0xFFFFFF);
    os << "quote = " << buf << "\n";
    snprintf(buf, sizeof(buf), "%08X", panel_color);
    os << "panel = " << buf << "\n";
    QByteArray out(os.str().c_str(), (int)os.str().size());
    return writeFile(path, out);
}
