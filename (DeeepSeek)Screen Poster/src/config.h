#pragma once
#include <string>
#include <map>

struct Config {
    int idle_timeout_seconds = 300;
    int slide_interval_seconds = 20;
    std::string transition_effect = "fade";   // fade | blinds | nonlinear | none
    int transition_duration_ms = 800;
    bool show_datetime = true;
    bool show_quote = true;

    std::string datetime_format = "yyyy年MM月dd日 hh:mm:ss"; // Qt date/time format
    std::string datetime_hpos = "left";   // left | center | right
    std::string datetime_vpos = "top";    // top | middle | bottom
    int datetime_font_size = 56;

    std::string quote_hpos = "left";
    std::string quote_vpos = "bottom";
    int quote_font_size = 34;
    std::string quote_author_pos = "right"; // right | below
    int quote_author_font_size = 20;
    std::string font_name = "Microsoft YaHei";

    unsigned int datetime_color = 0xFFFFFFFF;
    unsigned int quote_color = 0xFFFFFFFF;
    unsigned int panel_color = 0x80000000;

    bool load();
    bool save() const;
};
