SUBDIRS = ./Sources 

all all-pdf all-docx clean \
user-guide user-guide-pdf user-guide-docx \
: $(SUBDIRS)

$(SUBDIRS): FORCE
	make -C $@ $(MAKECMDGOALS)

FORCE:
