.PHONY: up up-ml up-full train down down-v

up:
	docker compose up --build

# banco + API + ML (sem front-end)
up-ml:
	docker compose --profile ml up --build

# tudo: banco + API + ML + front-end
up-full:
	docker compose --profile frontend --profile ml up --build

# gera o primeiro modelo (uma vez, depois que o banco estiver populado)
train:
	docker compose --profile jobs run --rm ml-train

down:
	docker compose --profile frontend --profile ml down

down-v:
	docker compose --profile frontend --profile ml down -v