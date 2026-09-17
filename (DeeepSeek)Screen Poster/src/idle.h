#pragma once
#include <QtGlobal>

// Returns the number of milliseconds since the last user input (mouse/keyboard).
// Returns 0 if the platform-specific backend is unavailable.
quint64 systemIdleMs();
