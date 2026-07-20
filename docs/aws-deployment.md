# Deploying Car Lookup to AWS (free tier)

The application is deployed as a Docker container on a single **EC2** virtual machine — the
smallest, cheapest way to host a container on AWS, and the one covered by the free tier.

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
   | Network settings | Tick **Allow HTTP traffic from the internet** |
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

Install the Compose plugin:

```bash
sudo mkdir -p /usr/local/lib/docker/cli-plugins
sudo curl -sSL \
  "https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64" \
  -o /usr/local/lib/docker/cli-plugins/docker-compose
sudo chmod +x /usr/local/lib/docker/cli-plugins/docker-compose
```

The `usermod` line only takes effect on a new login, so **close the browser terminal and connect
again**. Then check both tools answer:

```bash
docker version
docker compose version
```

---

## 4. Deploy the application

```bash
git clone https://github.com/<your-account>/<your-repo>.git
cd <your-repo>
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

The first build takes a few minutes on a `t3.micro`: it downloads the .NET SDK image and compiles
the app. Watch it finish, then confirm the container is healthy:

```bash
docker compose ps
curl -i http://localhost/health
```

You want `Up … (healthy)` and `HTTP/1.1 200 OK`.

> **If the build is killed part-way through**, the instance ran out of memory (`t3.micro` has 1 GB).
> Add swap once and rebuild:
>
> ```bash
> sudo dd if=/dev/zero of=/swapfile bs=1M count=2048
> sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile
> echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
> ```

---

## 5. Open it in a browser

Back in the EC2 console, copy the instance's **Public IPv4 address** and visit:

```
http://<public-ip>
```

Use `http://`, not `https://` — no TLS certificate is configured.

`restart: always` in `docker-compose.prod.yml` means the container comes back by itself whenever
Docker or the instance restarts.

---

## 6. Shipping an update

```bash
cd <your-repo>
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

**Why EC2 rather than something more managed?** App Runner and ECS Fargate would each host this
container with less setup, but neither is covered by the free tier. A single EC2 instance running
`docker compose` is the cheapest option that still demonstrates a real container deployment.

**Why build on the instance rather than push an image?** It keeps the deployment to one `git pull`
and avoids needing a container registry. For anything beyond a demo, the better shape is to build
the image in CI, push it to ECR, and have the instance pull a tagged image — that removes the SDK
and the source tree from the server entirely.
