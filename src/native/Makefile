#---------------------------------------------------------------------------------
# Switch-SMAPI Native Bootstrap
# Targets: Switch (ARM64 / Horizon OS)
# Toolchain: devkitPro devkitA64
#---------------------------------------------------------------------------------

ifeq ($(strip $(DEVKITPRO)),)
$(error "Set DEVKITPRO in your environment. export DEVKITPRO=/opt/devkitpro")
endif

include $(DEVKITPRO)/libnx/switch_rules

#---------------------------------------------------------------------------------
# Target configuration
#---------------------------------------------------------------------------------

TARGET      := SwitchSMAPI_Bootstrap
BUILD       := build
SOURCES     := source
INCLUDES    := include
OUTPUT      := output

ARCH        := -march=armv8-a+crc+crypto -mtune=cortex-a57 -mtp=soft

#---------------------------------------------------------------------------------
# Source collection — must precede INCLUDE/CFLAGS because := evaluates immediately
#---------------------------------------------------------------------------------

CFILES      := $(foreach dir,$(SOURCES),$(wildcard $(dir)/*.c))
CPPFILES    := $(foreach dir,$(SOURCES),$(wildcard $(dir)/*.cpp))
OFILES      := $(CPPFILES:%.cpp=$(BUILD)/%.o) $(CFILES:%.c=$(BUILD)/%.o)
DEPENDS     := $(OFILES:.o=.d)

INCLUDE     := $(foreach dir,$(INCLUDES),-I$(CURDIR)/$(dir)) \
               -I$(DEVKITPRO)/libnx/include

CFLAGS      := -g -Wall -Wextra -O2 -ffunction-sections -fdata-sections \
               $(ARCH) $(INCLUDE) -D__SWITCH__ -DSMAPI_NATIVE

CXXFLAGS    := $(CFLAGS) -std=c++17 -fno-rtti -fno-exceptions

LDFLAGS     := -specs=$(DEVKITPRO)/libnx/switch.specs \
               $(ARCH) -Wl,-Map,$(OUTPUT)/$(TARGET).map \
               -Wl,--gc-sections

LIBS        := -lnx

LIBPATHS    := -L$(DEVKITPRO)/libnx/lib

#---------------------------------------------------------------------------------
# Build rules
#---------------------------------------------------------------------------------

.PHONY: all clean debug output_dir

all: output_dir $(OUTPUT)/$(TARGET).nso
	@echo "Build complete: $(OUTPUT)/$(TARGET).nso"

debug: CFLAGS   += -DDEBUG -DSMAPI_DEBUG -g3 -O0
debug: CXXFLAGS += -DDEBUG -DSMAPI_DEBUG -g3 -O0
debug: all

output_dir:
	@mkdir -p $(OUTPUT)
	@mkdir -p $(BUILD)/source

$(OUTPUT)/$(TARGET).nso: $(OUTPUT)/$(TARGET).elf
	@echo "  ELF2NSO  $<"
	$(ELF2NSO) $< $@

$(OUTPUT)/$(TARGET).elf: $(OFILES)
	@echo "  LD       $@"
	$(CXX) $(LDFLAGS) $(OFILES) $(LIBPATHS) $(LIBS) -o $@

$(BUILD)/%.o: %.cpp
	@mkdir -p $(@D)
	@echo "  CXX      $<"
	$(CXX) $(CXXFLAGS) -MMD -MP -MF $(@:.o=.d) -c $< -o $@

$(BUILD)/%.o: %.c
	@mkdir -p $(@D)
	@echo "  CC       $<"
	$(CC) $(CFLAGS) -MMD -MP -MF $(@:.o=.d) -c $< -o $@

clean:
	@rm -rf $(BUILD) $(OUTPUT)
	@echo "Cleaned."

-include $(DEPENDS)
