
TARGET    := sgl_sim
BUILD_DIR := build

# Detect OS
ifeq ($(OS),Windows_NT)
    CONFIG = cmd /c copy /Y sgl_config.h ..\sgl\source\sgl_config.h
    SDL_COPY = cmd /c copy /Y sdl\bin\SDL2.dll $(BUILD_DIR)\SDL2.dll
    CLEAN = cmd /c if exist $(BUILD_DIR) rmdir /S /Q $(BUILD_DIR)
    MKDIR = cmd /c if not exist $(BUILD_DIR) mkdir $(BUILD_DIR)
else
    CONFIG = cp sgl_config.h ../sgl/source/sgl_config.h
    SDL_COPY = cp sdl/bin/SDL2.dll $(BUILD_DIR)/SDL2.dll
    CLEAN = rm -rf $(BUILD_DIR)
    MKDIR = mkdir -p $(BUILD_DIR)
endif


# toolchain
CC_PREFIX ?= 
CC = $(CC_PREFIX)gcc
AS = $(CC_PREFIX)gcc -x assembler-with-cpp
CP = $(CC_PREFIX)objcopy
SZ = $(CC_PREFIX)size
OD = $(CC_PREFIX)objdump
HEX = $(CP) -O ihex
BIN = $(CP) -O binary -S

CPATH     := -Isdl/include/SDL2      \
			 -I../sgl/source         \
			 -Iui_export         \
			 -I../sgl/source/include 

CFLAGS    := $(CPATH) -O2 -ffunction-sections -fdata-sections -Wunused-function -Wall -Wextra -std=c99 -g
LDFLAGS   := -Lsdl/lib -lmingw32 -lSDL2main -lSDL2 -mconsole -lm -ldinput8 -ldxguid -luser32 -lgdi32 -lwinmm -limm32 -lole32 -loleaut32 -lshell32 -lsetupapi -lversion -luuid -Wl,-Map=$(BUILD_DIR)/$(TARGET).map

UI_SOURCES := $(wildcard ui_export/*.c) \
              $(wildcard ui_export/screens/*.c) \
              $(wildcard ui_export/images/*.c) \
              $(wildcard ui_export/fonts/*.c)
			  
# 使用通配符，名称变了也不影响，不用一个一个添加源文件
SGL_SOURCES := $(wildcard ../sgl/source/fonts/*.c) \
			   $(wildcard ../sgl/source/core/*.c) \
			   $(wildcard ../sgl/source/draw/*.c) \
               $(wildcard ../sgl/source/mm/lwmem/*.c) \
			   $(wildcard ../sgl/source/widgets/*/*.c) \
			   $(wildcard ../sgl/source/widgets/chart/*/*.c) 


SOURCE    := main.c sgl_port_sdl2.c  \
			$(SGL_SOURCES) \
			$(UI_SOURCES) \
			

.PHONY: config all
all: config $(BUILD_DIR)/$(TARGET).exe elf_info

config:
	@echo "copy sgl_config.h..."
	@$(CONFIG)


# list of c and c++ program objects
OBJECTS = $(addprefix $(BUILD_DIR)/,$(notdir $(patsubst %.c, %.o, $(SOURCE))))
vpath %.c $(sort $(dir $(SOURCE)))


$(BUILD_DIR)/%.o: %.c Makefile | $(BUILD_DIR)
	@echo "CC   $<"
	@$(CC) -c $(CFLAGS) -MMD -MP \
		-MF  $(BUILD_DIR)/$(notdir $(<:.c=.d)) \
		-Wa,-a,-ad,-alms=$(BUILD_DIR)/$(notdir $(<:.c=.lst)) $< -o $@


$(BUILD_DIR)/$(TARGET).exe: $(OBJECTS) Makefile
	@echo "LD   $@"
	@$(CC) $(OBJECTS) $(LDFLAGS) -o $@
	@$(OD) $(BUILD_DIR)/$(TARGET).exe -xS > $(BUILD_DIR)/$(TARGET).s
	@echo "Build Successful!"


elf_info: $(BUILD_DIR)/$(TARGET).exe
	@echo "=================================================================="
	@$(SZ) $<
	@echo "=================================================================="


$(BUILD_DIR):
	@$(MKDIR)


# Pseudo command
.PHONY: clean run


run: $(BUILD_DIR)/$(TARGET).exe
	@$(SDL_COPY)
	@$(BUILD_DIR)/$(TARGET).exe


# clean command, delete build directory
clean:
	@$(CLEAN)
