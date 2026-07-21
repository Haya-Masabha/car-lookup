# Deploying Car Lookup to AWS (free tier)

Both containers — the Angular front end behind nginx and the ASP.NET Core API — run on a single
**EC2** virtual machine, the smallest and cheapest way to host containers on AWS and the one
covered by the free tier.

> **Before you start.** Two habits keep this free:
> 1. A **zero-spend budget** (Billing and Cost Management → Budgets) so AWS emails you the moment
>    anything is billed.
> 2. **Terminate the instance** when you no longer need the demo — see [Cleaning up](#cleaning-up).

Estimated time: about 20 minutes.

---

## 1. Launch the server

1. Sign in to the AWS console and confirm the **region** shown at the top right is the one you
   intend to use (for example *Frankfurt / eu-central-1*). Everything below must happen in that
   same region.
2. Search for **EC2** → **Instances** → **Launch instances**.
3. Fill in the form:

   | Field | Value |
   | --- | --- |
   | Name | `carlookup` |
   | Amazon Machine Image | **Amazon Linux 2023** (free-tier eligible) |
   | Instance type | **t3.micro** (or `t2.micro` if that is the free-tier type in your region) |
   | Key pair | *Proceed without a key pair* — you will connect through the browser |
   | Network settings | Tick **Allow HTTP traffic from the internet**, and leave **Allow SSH traffic from** ticked — the browser terminal connects over SSH, so port 22 must stay open |
   | Storage | Leave the default 8 GiB gp3 |

4. **Launch instance**, then open the instance and wait until *Instance state* is **Running** and
   both status checks have passed.

---

## 2. Connect to it

Select the instance → **Connect** → **EC2 Instance Connect** tab → **Connect**.

A terminal opens in the browser. No SSH key, no PuTTY.

---

## 3. Install Docker and Git

Paste this into that terminal:

```bash
sudo dnf update -y
sudo dnf install -y docker git
sudo systemctl enable --now docker
sudo usermod -aG docker ec2-user
```

Install the Compose plugin. Amazon Linux's Docker package does not look in
`/usr/local/lib/docker/cli-plugins`, so install it into the user plugin directory, which is always
searched and needs no `sudo`:

```bash
mkdir -p ~/.docker/cli-plugins
curl -sSL \
  "https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64" \
  -o ~/.docker/cli-plugins/docker-compose
chmod +x ~/.docker/cli-plugins/docker-compose
```

The `usermod` line only takes effect on a new login, so **close the browser terminal and connect
again**. Then check both tools answer:

```bash
docker version
docker compose version
```

> **If pasting mangles the command** — the browser terminal wraps pastes in escape codes and bash
> shows `$'\E[200~sudo': command not found`. Run `bind 'set enable-bracketed-paste off'` once per
> session to fix it.

---

## 4. Add swap, then deploy

A `t3.micro` has 1 GB of RAM and bundling the Angular app needs more than that, so give the build
some overflow room first. This is a one-off:

```bash
sudo dd if=/dev/zero of=/swapfile bs=1M count=2048
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

`free -h` should now show a 2 GiB `Swap:` row. Then deploy:

```bash
git clone https://github.com/Haya-Masabha/car-lookup.git
cd car-lookup
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

The first build takes several minutes on a `t3.micro`: it downloads the .NET SDK and Node images,
compiles the API and bundles the Angular app. Watch it finish, then confirm both containers are
healthy:

```bash
docker compose ps
curl -i http://localhost/health          # nginx
curl -s http://localhost/api/vehicles/years | head -c 80   # API through the proxy
```

You want `Up … (healthy)` for both services and `HTTP/1.1 200 OK`.

> **If the build is still killed part-way through**, the swap step above was skipped or did not
> take effect. Check `free -h`, then re-run the build.

---

## 5. Open it in a browser

Back in the EC2 console, copy the instance's **Public IPv4 address** and visit:

```
http://<public-ip>
```

Use `http://`, not `https://` — no TLS certificate is configured.

`restart: always` in `docker-compose.prod.yml` means both containers come back by themselves
whenever Docker or the instance restarts. That file also stops publishing the API's own port, so
the only way in from the internet is through nginx on port 80.

---

## 6. Shipping an update

```bash
cd car-lookup
git pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

---

## Cleaning up

An idle instance still consumes free-tier hours, and charges begin once those run out.

**Terminate** the instance when the demo is no longer needed: EC2 → Instances → select
`carlookup` → **Instance state** → **Terminate instance**. This deletes the machine and its disk.

To pause instead of delete, use **Stop instance** — compute is no longer billed, but the attached
disk still is, and the public IP changes on the next start.

---

## Design notes

**Why EC2 rather than something more managed?** App Runner and ECS Fargate would each host these
containers with less setup, but neither is covered by the free tier. A single EC2 instance running
`docker compose` is the cheapest option that still demonstrates a real container deployment.

**Why serve the SPA from nginx instead of from the API?** Keeping them separate means each tier
scales and deploys on its own, nginx does the static-file serving it is good at, and the proxy
makes every browser request same-origin — so there is no CORS configuration in production.

**Why build on the instance rather than push an image?** It keeps the deployment to one `git pull`
and avoids needing a container registry. For anything beyond a demo, the better shape is to build
both images in CI, push them to ECR, and have the instance pull tagged images — that removes the
SDK, Node and the source tree from the server entirely.
