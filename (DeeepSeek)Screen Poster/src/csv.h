#pragma once
#include <string>
#include <vector>

std::vector<std::string> parse_csv_line(const std::string& line);
std::string csv_escape(const std::string& s);
std::string csv_join(const std::vector<std::string>& fields);
