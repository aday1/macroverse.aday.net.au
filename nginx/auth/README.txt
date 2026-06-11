Basic auth for macroverse-private.aday.net.au (private :aday track).

Credentials are NEVER committed. Store only in a password manager and GitHub Actions secret ADAY_AUTH_PASSWORD.

Regenerate htpasswd (password required via env):

  ADAY_AUTH_PASSWORD='(from password manager)' bash ops/ensure-aday-password.sh

Interactive:

  bash ops/set-aday-password.sh

Mount path in container:

  /etc/nginx/auth/aday.htpasswd

All paths on the private lane require basic auth, including vj-output.html and audience stream URLs.