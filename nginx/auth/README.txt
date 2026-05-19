Basic auth for macroverse-aday.aday.net.au (aday track).

Committed default (rotate on production):

  user: aday
  pass: macroverse

Regenerate:

  htpasswd -c aday.htpasswd your_username

Or: bash ops/set-aday-password.sh your_username

Mount path in container:

  /etc/nginx/auth/aday.htpasswd
