FROM postgres:17.11-alpine3.24@sha256:266d3c257594e195b309ae9d1805e54c788f1d7b375f56bf6c367aac6c917e7b
RUN apk add --no-cache age aws-cli
COPY backup.sh restore-drill.sh /opt/backup/
RUN chmod 755 /opt/backup/*.sh
USER postgres
ENTRYPOINT ["/bin/sh", "/opt/backup/backup.sh"]
