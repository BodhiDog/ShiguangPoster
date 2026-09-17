#include "csv.h"

std::vector<std::string> parse_csv_line(const std::string& line) {
    std::vector<std::string> out;
    std::string cur;
    bool inQuotes = false;
    for (size_t i = 0; i < line.size(); ++i) {
        char c = line[i];
        if (inQuotes) {
            if (c == '"') {
                if (i + 1 < line.size() && line[i + 1] == '"') { cur += '"'; ++i; }
                else inQuotes = false;
            } else cur += c;
        } else {
            if (c == '"') inQuotes = true;
            else if (c == ',') { out.push_back(cur); cur.clear(); }
            else cur += c;
        }
    }
    out.push_back(cur);
    return out;
}

std::string csv_escape(const std::string& s) {
    bool need = s.find_first_of(",\"\r\n") != std::string::npos;
    if (!need) return s;
    std::string r = "\"";
    for (char c : s) {
        if (c == '"') r += "\"\"";
        else r += c;
    }
    r += "\"";
    return r;
}

std::string csv_join(const std::vector<std::string>& fields) {
    std::string r;
    for (size_t i = 0; i < fields.size(); ++i) {
        if (i) r += ",";
        r += csv_escape(fields[i]);
    }
    return r;
}
