# Base comum: dependências do pacote (pandas, LightGBM, psycopg...).
FROM python:3.12-slim AS base
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
# libgomp1: runtime OpenMP exigido pelo LightGBM
RUN apt-get update && apt-get install -y --no-install-recommends libgomp1 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY pyproject.toml ./
COPY src ./src
RUN pip install --no-cache-dir ".[api,sql]"

# ---- seed: gera os dados fictícios uma vez, no build (determinístico) ----
FROM base AS mockdata
RUN redistml mock generate --out /seed && find /seed -maxdepth 1 -type f -delete

# ---- serviço one-shot: aplica o seed no banco vazio ----
FROM postgres:16 AS seed
COPY --from=mockdata /seed/backend /seed
COPY docker/seed.sh /usr/local/bin/seed.sh
RUN chmod +x /usr/local/bin/seed.sh
WORKDIR /seed
ENTRYPOINT ["/usr/local/bin/seed.sh"]

# ---- API do ML (imagem dos serviços `ml` e `ml-train`; último estágio = alvo padrão) ----
FROM base AS runtime
RUN useradd -r -u 10001 mlsvc && mkdir -p /models && chown mlsvc /models
USER mlsvc
ENV ML_MODELS_DIR=/models
EXPOSE 8000
HEALTHCHECK --interval=30s --timeout=5s CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health')"
CMD ["uvicorn", "redistml.api:app_factory", "--factory", "--host", "0.0.0.0", "--port", "8000"]
