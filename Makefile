.PHONY: up up-full down down-v

up:
	docker compose up --build

up-full:
	docker compose --profile frontend up --build

down:
	docker compose down

down-v:
	docker compose down -v
