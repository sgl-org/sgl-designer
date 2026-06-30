#include <sgl_core.h>
#include <sgl_font.h>

#ifndef CONFIG_SGL_FONT_SGL_FONT_24
#define CONFIG_SGL_FONT_SGL_FONT_24 1
#endif

#if (CONFIG_SGL_FONT_SGL_FONT_24)
static const uint8_t font_bitmap[] = {

};

static const sgl_font_table_t font_table[] = {
    {.bitmap_index = 0, .adv_w = 0, .box_w = 0, .box_h = 0, .ofs_x = 0, .ofs_y = 0},
};

static const sgl_font_unicode_t font_unicode[] = {
};

const sgl_font_t font_sgl_font_24 = {
    .bitmap = font_bitmap, .table = font_table, .font_table_size = SGL_ARRAY_SIZE(font_table),
    .font_height = 24, .base_line = 6, .bpp = 4, .compress = 0,
    .unicode = font_unicode, .unicode_num = SGL_ARRAY_SIZE(font_unicode),
};
#endif
