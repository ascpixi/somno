#pragma once

#include <stdint.h>
#include <string>

constexpr uint32_t modulus() {
    return 0x7fffffff;
}

constexpr uint32_t prng(const uint32_t input) {
    return (input * 48271) % modulus();
}

template<size_t N>
constexpr uint32_t seed(const char(&entropy)[N], const uint32_t iv = 0) {
    auto value{ iv };
    for (size_t i{ 0 }; i < N; i++) {
        // Xor 1st byte of seed with input byte
        value = (value & ((~0) << 8)) | ((value & 0xFF) ^ entropy[i]);
        // Rotate left 1 byte
        value = value << 8 | value >> ((sizeof(value) * 8) - 8);
    }
    // The seed is required to be less than the modulus and odd
    while (value > modulus()) value = value >> 1;
    return value << 1 | 1;
}

template<typename T, size_t N>
struct encrypted {
    int seed;
    T data[N];
};

template<size_t N>
constexpr auto crypt(const char(&input)[N], const uint32_t seed = 0) {
    encrypted<char, N> blob{};
    blob.seed = seed;
    for (uint32_t index{ 0 }, stream{ seed }; index < N; index++) {
        blob.data[index] = input[index] ^ stream;
        stream = prng(stream);
    }
    return blob;
}

wchar_t* AsciiToUnicode(const char* charArray) {
    wchar_t* wString = new wchar_t[4096];
    MultiByteToWideChar(CP_ACP, 0, charArray, -1, wString, 4096);
    return wString;
}

#define ENC_LPWSTR(STRING) ([&] {                               \
    constexpr auto _{ crypt(STRING, seed(__FILE__, __LINE__)) }; \
    return AsciiToUnicode(crypt(_.data, _.seed).data);            \
}())

#define ENC_STRING(STRING) ([&] {                               \
    constexpr auto _{ crypt(STRING, seed(__FILE__, __LINE__)) }; \
    return (crypt(_.data, _.seed).data);            \
}())