# Hosting and releases

## Running in a container

A prebuilt image (linux/amd64 and linux/arm64) is published to Docker Hub on every push to `master`
and on version tags:

```bash
docker run --rm -p 8080:8080 kvloover/bikegeo:latest
```

or build it yourself from the `Dockerfile`:

```bash
docker build -t bikegeo .
docker run --rm -p 8080:8080 bikegeo
```

then open <http://localhost:8080>. The image listens on plain HTTP on port 8080 and expects TLS to
be handled by whatever sits in front of it (an App Service, Container Apps, a reverse proxy). It
writes nothing to disk, so it runs happily with `--read-only` and without a volume.

One thing to know before hosting it publicly: **Add geometry → fetch from a URL** makes the server
request whatever address a visitor types in. Restrict or disable it on a public deployment.


## Releases

Releases are cut by pushing a version tag:

```bash
git tag v1.2.0
git push origin v1.2.0
```

That one tag does two things: the **Release** workflow creates a GitHub Release whose notes are
generated from the pull requests merged since the previous tag, and the **Docker image** workflow
publishes `1.2.0`, `1.2` and `latest` images. Label pull requests (`feature`, `fix`, `data`,
`docs`) to have them grouped in the notes, or `skip-changelog` to leave one out; the grouping lives
in [`.github/release.yml`](../.github/release.yml). A tag with a suffix, such as `v1.2.0-rc.1`, is
marked as a pre-release.

