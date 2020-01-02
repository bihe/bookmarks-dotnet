PROJECTNAME=$(shell basename "$(PWD)")

# Make is verbose in Linux. Make it silent.
MAKEFLAGS += --silent

VERSION="1.0.0-"
COMMIT=`git rev-parse HEAD | cut -c 1-8`
BUILD=`date -u +%Y%m%d.%H%M%S`

docker-build:
	@-$(MAKE) -s __docker-build

docker-run:
	@-$(MAKE) -s __docker-run

frontend:
	@-$(MAKE) do-frontend-build

__docker-build:
	@echo " ... building docker image"
	docker build -t bookmarks .

__docker-run:
	@echo " ... running docker image"
	docker run -it -p 127.0.0.1:3000:3000 -v "$(PWD)/src/Api/_etc":/opt/bookmarks.binggl.net/_etc bookmarks

do-frontend-build:
	@echo "  >  Building angular frontend ..."
	cd ./src/UI;	npm install && npm run build -- --prod --base-href /ui/

.PHONY: compile release test run clean coverage

